# Teacher Document Permissions And UI Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix teacher document permissions, teacher assignment refresh behavior, token usage sorting, landing marquee behavior, and upload-by-chapter UX.

**Architecture:** Put permission rules in `BLL\Services\DocumentService.cs` so UI hiding cannot bypass security. Keep MVC controllers thin, update Razor views for role-appropriate actions, and add BLL tests around every changed business rule.

**Tech Stack:** ASP.NET Core MVC, Razor views, Entity Framework Core repositories, xUnit tests in `BLL.Tests`.

## Global Constraints

- Assume "trưởng bộ môn" means the teacher whose `Department.ManagerTeacherId` matches the current teacher id.
- A normal assigned teacher may view/download existing documents for assigned subjects, but may not upload, edit, reindex, or archive.
- Only department managers may upload, edit, reindex, and archive documents for subjects in their managed department.
- After removing a teacher assignment, the assignment form must immediately allow selecting the teacher/subject again without requiring logout/login or a full browser restart.
- Token usage daily rows must default to the current month and sort by `TotalTokens` descending, then by current/recent date descending.
- Landing marquee text is information, not buttons: it should keep moving, should not pause on hover, and should not brighten like an interactive control.
- Upload document flow must allow assigning a document to a chapter, and the chapter selector should be clearly tied to the selected subject.
- Preserve existing MVC structure and avoid broad refactors.
- Use UTF-8 Vietnamese text when touching corrupted user-facing copy.

---

## File Structure

- Modify `BLL\Services\DocumentService.cs`: add department-manager permission helpers and apply them to upload/edit/archive/reindex while keeping view/download access for assigned teachers.
- Modify `BLL\Interfaces\IDocumentService.cs`: add any small helper surface only if the controller needs a permission flag for UI.
- Modify `finalProject\Controllers\TeacherDocumentsController.cs`: restrict mutating actions through service checks, prepare upload/edit view models, and return clear forbidden/model errors.
- Modify `finalProject\ViewModels\DocumentUploadViewModel.cs`: keep chapter support and add subject/chapter metadata only if needed for dependent dropdown filtering.
- Modify `finalProject\Views\TeacherDocuments\Upload.cshtml`: update Vietnamese labels and add subject-aware chapter UX.
- Modify `finalProject\Views\TeacherDocuments\Index.cshtml`, `Details.cshtml`, `Edit.cshtml`: hide or show upload/edit/archive/reindex actions according to department-manager capability.
- Modify `BLL\Services\TeacherAssignmentService.cs`: make teacher/subject options exclude only currently assigned pairs in a way that refreshes after removal.
- Modify `finalProject\Controllers\TeacherAssignmentsController.cs`: after removal, rebuild the index model from fresh data and keep the selected subject/teacher available immediately.
- Modify `finalProject\Views\TeacherAssignments\Index.cshtml`: add optional pair-aware filtering UX if backend option changes need support.
- Modify `BLL\Services\AiUsageService.cs`: sort daily token rows by most tokens, then newest date.
- Modify `finalProject\Controllers\TokenUsageController.cs`: keep default month as current UTC month.
- Modify `finalProject\Views\TokenUsage\Index.cshtml`: update labels if needed so sort order is obvious.
- Modify `finalProject\Views\Home\Index.cshtml`: use non-link/non-button semantics for marquee items and fix corrupted Vietnamese copy touched in this area.
- Modify `finalProject\wwwroot\css\site.css`: remove hover pause and hover-brightening styles from `.studymate-feature-pill`; keep reduced-motion behavior.
- Modify `BLL.Tests\DocumentServiceTests.cs`: add permission tests for assigned teacher view-only and department-manager write access.
- Modify `BLL.Tests\TeacherAssignmentServiceTests.cs`: add option refresh test after removal.
- Modify `BLL.Tests\AiUsageServiceTests.cs`: add daily sort test.

---

### Task 1: Lock Document Write Permissions To Department Managers

**Files:**
- Modify: `BLL\Services\DocumentService.cs`
- Modify: `BLL\Interfaces\IDocumentService.cs`
- Test: `BLL.Tests\DocumentServiceTests.cs`

**Interfaces:**
- Consumes: `TeacherSubject.Subject.Department.ManagerTeacherId`
- Produces: service behavior where `UploadDocumentAsync`, `UpdateDocumentContentAsync`, `ArchiveDocumentAsync`, and `ReindexDocumentAsync` require the teacher to manage the selected document subject department.

- [ ] **Step 1: Write failing tests for manager-only upload/edit**

Add tests in `BLL.Tests\DocumentServiceTests.cs`:

```csharp
[Fact]
public async Task UploadDocumentAsync_Throws_WhenAssignedTeacherIsNotDepartmentManager()
{
    var service = new DocumentService(
        new FakeDocumentRepository(Array.Empty<Document>()),
        new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 5),
        new FakeChunkingService());

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UploadDocumentAsync(
        new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
        new CreateDocumentDto
        {
            SubjectId = 10,
            UploadedByTeacherId = 2,
            Title = "Forbidden",
            FileName = "forbidden.pdf",
            ContentType = "application/pdf",
            FileContent = "content"u8.ToArray(),
            StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        }));
}

[Fact]
public async Task UploadDocumentAsync_AllowsDepartmentManagerForManagedSubject()
{
    var repository = new FakeDocumentRepository(Array.Empty<Document>());
    var service = new DocumentService(
        repository,
        new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
        new FakeChunkingService());

    var documentId = await service.UploadDocumentAsync(
        new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
        new CreateDocumentDto
        {
            SubjectId = 10,
            UploadedByTeacherId = 2,
            Title = "Allowed",
            FileName = "allowed.pdf",
            ContentType = "application/pdf",
            FileSize = 7,
            FileContent = "content"u8.ToArray(),
            StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

    Assert.True(documentId > 0);
    Assert.Single(repository.SavedDocuments);
}
```

- [ ] **Step 2: Run the focused failing tests**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj --filter "UploadDocumentAsync_Throws_WhenAssignedTeacherIsNotDepartmentManager|UploadDocumentAsync_AllowsDepartmentManagerForManagedSubject"
```

Expected: first new test fails because upload currently allows any assigned teacher.

- [ ] **Step 3: Implement manager permission helper**

In `BLL\Services\DocumentService.cs`, add:

```csharp
private async Task<Subject> GetManagedAssignedSubjectAsync(
    CurrentUserDto currentUser,
    int subjectId,
    CancellationToken cancellationToken)
{
    AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

    var assignments = await _teacherSubjectRepository.GetByTeacherAsync(currentUser.UserId, cancellationToken);
    var subject = assignments
        .Select(assignment => assignment.Subject)
        .FirstOrDefault(subject => subject?.Id == subjectId && subject.IsActive);

    if (subject is null)
    {
        throw new UnauthorizedAccessException("Teachers can only use assigned active Subjects.");
    }

    if (subject.Department?.ManagerTeacherId != currentUser.UserId)
    {
        throw new UnauthorizedAccessException("Only the department manager can upload or modify documents for this Subject.");
    }

    return subject;
}
```

Then update `UploadDocumentAsync`, `UpdateDocumentAsync`, `UpdateDocumentContentAsync`, `ArchiveDocumentAsync`, and `ReindexDocumentAsync` to call this helper before changing data. Keep `GetDocumentByIdAsync` and `Download` view behavior unchanged for assigned/owned teacher access.

- [ ] **Step 4: Update fake repository subject department data**

In `BLL.Tests\DocumentServiceTests.cs`, update `FakeTeacherSubjectRepository` constructor:

```csharp
private readonly int? _managerTeacherId;

public FakeTeacherSubjectRepository(IEnumerable<int>? assignedSubjectIds = null, int? managerTeacherId = null)
{
    _assignedSubjectIds = assignedSubjectIds?.ToHashSet() ?? new HashSet<int> { 10 };
    _managerTeacherId = managerTeacherId;
}
```

Set the fake subject department:

```csharp
Department = new Department { ManagerTeacherId = _managerTeacherId ?? teacherId }
```

- [ ] **Step 5: Run document tests**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj --filter "FullyQualifiedName~DocumentServiceTests"
```

Expected: PASS.

---

### Task 2: Make Teacher Document UI View-Only For Non-Managers

**Files:**
- Modify: `finalProject\Controllers\TeacherDocumentsController.cs`
- Modify: `finalProject\ViewModels\TeacherDocumentsIndexViewModel.cs`
- Modify: `finalProject\Views\TeacherDocuments\Index.cshtml`
- Modify: `finalProject\Views\TeacherDocuments\Details.cshtml`

**Interfaces:**
- Consumes: department-manager permission from Task 1.
- Produces: normal assigned teachers see documents/download only; department managers see upload/edit/archive/reindex actions.

- [ ] **Step 1: Add view model flag**

In `finalProject\ViewModels\TeacherDocumentsIndexViewModel.cs`, add:

```csharp
public bool CanManageDocuments { get; set; }
```

- [ ] **Step 2: Populate the flag**

In `TeacherDocumentsController.Index`, compute `CanManageDocuments` from upload options:

```csharp
var uploadOptions = await _documentService.GetUploadOptionsForTeacherAsync(currentUser, cancellationToken);
var canManageDocuments = uploadOptions.Subjects.Any();
```

If Task 1 changes upload options to return only manager-owned subjects, use that result directly. Set `CanManageDocuments = canManageDocuments`.

- [ ] **Step 3: Hide upload button and mutating row actions**

In `finalProject\Views\TeacherDocuments\Index.cshtml`, wrap upload/edit/archive/reindex controls:

```cshtml
@if (Model.CanManageDocuments)
{
    <a class="btn btn-primary admin-primary-action" asp-action="Upload">Tải tài liệu</a>
}
```

For normal teachers, keep details and download links visible.

- [ ] **Step 4: Protect direct GET Upload/Edit routes**

In `TeacherDocumentsController.Upload` GET, after building the model:

```csharp
if (model.SubjectOptions.Count == 0)
{
    TempData["StatusMessage"] = "Chỉ trưởng bộ môn mới được tải tài liệu lên.";
    return RedirectToAction(nameof(Index));
}
```

In Edit GET/POST, rely on Task 1 service enforcement and keep `Forbid()` for `UnauthorizedAccessException`.

- [ ] **Step 5: Manual MVC verification**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj
dotnet run --project finalProject\finalProject.csproj
```

Expected: normal teacher can open list/details/download, cannot see upload/edit/archive/reindex, and direct upload/edit returns redirect/forbid.

---

### Task 3: Refresh Teacher Assignment Options Immediately After Removal

**Files:**
- Modify: `BLL\Services\TeacherAssignmentService.cs`
- Modify: `finalProject\Controllers\TeacherAssignmentsController.cs`
- Test: `BLL.Tests\TeacherAssignmentServiceTests.cs`

**Interfaces:**
- Consumes: `ITeacherSubjectRepository.GetAllAsync`, `GetTeacherOptionsAsync`, `GetSubjectOptionsAsync`.
- Produces: after `RemoveTeacherFromSubjectAsync`, fresh teacher and subject options include the removed pair immediately.

- [ ] **Step 1: Write failing service test**

Add:

```csharp
[Fact]
public async Task GetSubjectOptionsAsync_IncludesSubjectImmediatelyAfterAssignmentRemoval()
{
    var teacher = new User { Id = 2, FullName = "Teacher A", Role = UserRole.Teacher, IsActive = true };
    var subject = new Subject { Id = 10, Code = "PRN222", Name = "PRN222", IsActive = true };
    var assignment = new TeacherSubject { Id = 7, TeacherId = teacher.Id, Teacher = teacher, SubjectId = subject.Id, Subject = subject };
    var repository = new FakeTeacherSubjectRepository(new[] { assignment });
    var subjectService = new FakeSubjectService(new[] { new SubjectDto { Id = 10, Code = "PRN222", Name = "PRN222", IsActive = true } });
    var service = new TeacherAssignmentService(subjectService, repository, new FakeUserRepository());

    await service.RemoveTeacherFromSubjectAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, assignment.Id);

    var options = await service.GetSubjectOptionsAsync();

    Assert.Contains(options, option => option.Id == 10);
}
```

- [ ] **Step 2: Run focused assignment tests**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj --filter "FullyQualifiedName~TeacherAssignmentServiceTests"
```

Expected: PASS after fake setup supports subjects. If current UI bug is controller-side, service test may already pass; continue with controller refresh.

- [ ] **Step 3: Rebuild fresh index model after removal**

In `TeacherAssignmentsController.RemoveAssignment`, replace redirect with fresh view return:

```csharp
await _teacherAssignmentService.RemoveTeacherFromSubjectAsync(GetCurrentUser(), id, cancellationToken);
TempData["SuccessMessage"] = "Đã hủy phân công giảng viên khỏi môn học.";

return View(nameof(Index), new TeacherAssignmentsIndexViewModel
{
    Form = new TeacherAssignmentFormViewModel
    {
        TeacherOptions = await GetTeacherOptionsAsync(cancellationToken),
        SubjectOptions = await GetSubjectOptionsAsync(cancellationToken)
    },
    Assignments = await _teacherAssignmentService.GetAssignmentsAsync(cancellationToken)
});
```

- [ ] **Step 4: Verify in browser**

Run app and remove an assignment. Expected: the page immediately shows the removed teacher and subject selectable again in the form.

---

### Task 4: Sort Token Usage By Most Tokens And Current Date

**Files:**
- Modify: `BLL\Services\AiUsageService.cs`
- Modify: `finalProject\Views\TokenUsage\Index.cshtml`
- Test: `BLL.Tests\AiUsageServiceTests.cs`

**Interfaces:**
- Consumes: `AiUsageDashboardDto.DailySummaries`
- Produces: daily table sorted by `TotalTokens desc`, then `Date desc`.

- [ ] **Step 1: Write failing sort test**

Add to `BLL.Tests\AiUsageServiceTests.cs`:

```csharp
[Fact]
public async Task GetDashboardAsync_SortsDailySummariesByTotalTokensThenNewestDate()
{
    var logs = new[]
    {
        new AiUsageLog { CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc), ModelName = "gemini", PromptTokens = 10, CompletionTokens = 5 },
        new AiUsageLog { CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), ModelName = "gemini", PromptTokens = 10, CompletionTokens = 5 },
        new AiUsageLog { CreatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), ModelName = "gemini", PromptTokens = 40, CompletionTokens = 10 }
    };
    var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

    var dashboard = await service.GetDashboardAsync(
        new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
        new AiUsageDashboardFilterDto { Year = 2026, Month = 7 });

    Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[0].Date);
    Assert.Equal(new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[1].Date);
}
```

- [ ] **Step 2: Implement sort**

In `AiUsageService.GetDashboardAsync`, after building `dailySummaries`, apply:

```csharp
.OrderByDescending(summary => summary.TotalTokens)
.ThenByDescending(summary => summary.Date)
.ToList();
```

- [ ] **Step 3: Update table heading**

In `finalProject\Views\TokenUsage\Index.cshtml`, change daily section helper text to clarify the order:

```cshtml
<h2 class="section-title">Token theo ngày, nhiều nhất trước</h2>
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj --filter "FullyQualifiedName~AiUsageServiceTests"
```

Expected: PASS.

---

### Task 5: Make Landing Marquee Non-Interactive Information

**Files:**
- Modify: `finalProject\Views\Home\Index.cshtml`
- Modify: `finalProject\wwwroot\css\site.css`

**Interfaces:**
- Consumes: existing `.studymate-marquee-track` animation.
- Produces: marquee keeps moving and no longer looks clickable.

- [ ] **Step 1: Remove hover pause**

Delete this block from `site.css`:

```css
.studymate-marquee:hover .studymate-marquee-track,
.studymate-marquee.is-paused .studymate-marquee-track {
  animation-play-state: paused;
}
```

- [ ] **Step 2: Remove hover affordance from feature pills**

Replace:

```css
.studymate-contact-pill:hover,
.studymate-feature-pill:hover {
  border-color: rgba(37, 99, 235, 0.38);
  color: #1d4ed8;
  transform: translateY(-1px);
}
```

with:

```css
.studymate-contact-pill:hover {
  border-color: rgba(37, 99, 235, 0.38);
  color: #1d4ed8;
  transform: translateY(-1px);
}
```

Delete:

```css
.studymate-feature-pill:hover .studymate-feature-icon {
  background: #dbeafe;
  color: #1d4ed8;
}
```

- [ ] **Step 3: Clarify semantics**

In `Home\Index.cshtml`, add `aria-hidden="true"` to duplicated marquee set if duplication is kept, or keep only informational `div` elements with no `tabindex`, no link, and no button behavior.

- [ ] **Step 4: Visual verification**

Run app and open home page. Expected: feature text moves continuously, hover does not pause, brighten, or imply clickability.

---

### Task 6: Improve Upload By Chapter UX

**Files:**
- Modify: `finalProject\ViewModels\DocumentUploadViewModel.cs`
- Modify: `finalProject\Controllers\TeacherDocumentsController.cs`
- Modify: `finalProject\Views\TeacherDocuments\Upload.cshtml`
- Test: `BLL.Tests\DocumentServiceTests.cs`

**Interfaces:**
- Consumes: `DocumentUploadOptionsDto.Subjects` and `.Chapters`.
- Produces: upload form lets the department manager choose a subject and then choose only chapters belonging to that subject.

- [ ] **Step 1: Preserve chapter-subject metadata**

Add to `DocumentUploadViewModel`:

```csharp
public IReadOnlyList<DocumentChapterOptionViewModel> ChapterMetadata { get; set; } = Array.Empty<DocumentChapterOptionViewModel>();
```

Use existing `DocumentChapterOptionViewModel` if it already has `Id`, `SubjectId`, and `DisplayName`. If not, add those properties there.

- [ ] **Step 2: Populate metadata**

In `BuildUploadViewModelAsync`, after `model.ChapterOptions`, set:

```csharp
model.ChapterMetadata = options.Chapters
    .Select(chapter => new DocumentChapterOptionViewModel
    {
        Id = chapter.Id,
        SubjectId = chapter.SubjectId,
        DisplayName = chapter.DisplayName
    })
    .ToList();
```

- [ ] **Step 3: Add subject-aware filtering script**

In `Upload.cshtml`, render options with `data-subject-id`:

```cshtml
<select asp-for="ChapterId" class="form-select" data-chapter-select>
    <option value="">Không gán chương</option>
    @foreach (var chapter in Model.ChapterMetadata)
    {
        <option value="@chapter.Id" data-subject-id="@chapter.SubjectId">@chapter.DisplayName</option>
    }
</select>
```

Add page script:

```html
<script>
(() => {
  const subject = document.querySelector('[name="SubjectId"]');
  const chapter = document.querySelector('[data-chapter-select]');
  if (!subject || !chapter) return;

  const syncChapters = () => {
    const subjectId = subject.value;
    for (const option of chapter.options) {
      if (!option.value) {
        option.hidden = false;
        continue;
      }
      option.hidden = option.dataset.subjectId !== subjectId;
    }
    if (chapter.selectedOptions[0]?.hidden) {
      chapter.value = "";
    }
  };

  subject.addEventListener('change', syncChapters);
  syncChapters();
})();
</script>
```

- [ ] **Step 4: Verify service validation**

Use existing `ValidateChapterForTeacherSubjectAsync` test coverage and add:

```csharp
[Fact]
public async Task UploadDocumentAsync_Throws_WhenChapterDoesNotBelongToSelectedSubject()
{
    var service = new DocumentService(
        new FakeDocumentRepository(Array.Empty<Document>()),
        new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
        new FakeChunkingService());

    await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
        new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
        new CreateDocumentDto
        {
            SubjectId = 10,
            ChapterId = 999,
            UploadedByTeacherId = 2,
            Title = "Wrong chapter",
            FileName = "wrong.pdf",
            ContentType = "application/pdf",
            FileContent = "content"u8.ToArray(),
            StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        }));
}
```

- [ ] **Step 5: Run tests and manual upload check**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj --filter "FullyQualifiedName~DocumentServiceTests"
```

Expected: PASS, and upload form filters chapters when subject changes.

---

### Task 7: Fix Vietnamese Copy Touched By These Screens

**Files:**
- Modify: `finalProject\Views\Home\Index.cshtml`
- Modify: `finalProject\Views\TeacherDocuments\Upload.cshtml`
- Modify: `finalProject\Views\TeacherAssignments\Index.cshtml`
- Modify: `finalProject\Views\TokenUsage\Index.cshtml`
- Modify: `finalProject\Controllers\TeacherDocumentsController.cs`
- Modify: `finalProject\Controllers\TeacherAssignmentsController.cs`
- Modify: `BLL\Services\DocumentService.cs`

**Interfaces:**
- Consumes: current user-facing strings in affected files.
- Produces: readable UTF-8 Vietnamese labels/messages on changed screens.

- [ ] **Step 1: Replace corrupted labels in touched views**

Use exact readable text such as:

```cshtml
ViewData["Title"] = "Tải tài liệu lên";
<label asp-for="SubjectId" class="form-label">Môn học</label>
<label asp-for="ChapterId" class="form-label">Chương</label>
<button type="submit" class="btn btn-primary admin-primary-action">Upload</button>
```

- [ ] **Step 2: Replace corrupted controller messages**

Use exact readable text such as:

```csharp
TempData["StatusMessage"] = $"Đã upload tài liệu #{documentId}. Hệ thống đã xử lý chunk và index theo cấu hình hiện tại.";
TempData["SuccessMessage"] = "Đã hủy phân công giảng viên khỏi môn học.";
```

- [ ] **Step 3: Check file encoding**

Run:

```powershell
rg "Ã|Ä|áº|âœ|Táº|MÃ|Giáº" finalProject BLL
```

Expected: no matches in files touched by this plan. Existing untouched files may still match and can be fixed in a separate cleanup.

---

### Task 8: Full Verification

**Files:**
- No code changes unless verification exposes a failure.

**Interfaces:**
- Consumes: all previous tasks.
- Produces: confidence that changes work together.

- [ ] **Step 1: Run all BLL tests**

Run:

```powershell
dotnet test BLL.Tests\BLL.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Build MVC project**

Run:

```powershell
dotnet build finalProject.slnx
```

Expected: build succeeds with 0 errors.

- [ ] **Step 3: Manual role checks**

Run:

```powershell
dotnet run --project finalProject\finalProject.csproj
```

Check:

- Assigned non-manager teacher can view/download but cannot upload/edit/archive/reindex.
- Department manager can upload/edit/archive/reindex only for managed department subjects.
- Removing an assignment immediately refreshes selectable teacher/subject options.
- Token usage daily table shows the current month by default and rows ordered by highest token usage, newest date second.
- Home marquee continues moving on hover and does not look clickable.
- Upload form filters chapters based on selected subject.

---

## Self-Review

- Spec coverage: all six user-reported items are covered by Tasks 1-6, with encoding cleanup in Task 7 and full verification in Task 8.
- Placeholder scan: no unfinished placeholder steps are present.
- Type consistency: service/controller/view model names match existing project files; the only new property is `CanManageDocuments` on `TeacherDocumentsIndexViewModel` and optional `ChapterMetadata` on `DocumentUploadViewModel`.
