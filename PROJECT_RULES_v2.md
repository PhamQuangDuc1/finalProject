# PROJECT_RULES.md

Completed through Stage 12.

Technology: ASP.NET Core MVC, C#, EF Core, SQL Server, SignalR.

Architecture: Presentation -> BLL -> DAL.

Admin: manage system, no upload.
Teacher: upload/manage own documents only.
Student: read-only.

Business Rules:

* One Department has one Manager Teacher.
* Chunk settings only by Admin.
* Soft Delete for Archive.
* Authorization enforced in BLL.
* Subscription Packages managed only by Admin.
* Each User may have at most one Active UserSubscription at a time.
* Payment is auto-marked as Pending on creation, then Completed or Failed by Admin action.
* UserSubscription is activated only when the related Payment is Completed.

Never change architecture, database schema, authentication, authorization, repository/service interfaces without request.



\## UI \& Localization Rules (MANDATORY)



The entire user interface must use proper Vietnamese with full diacritics.



Examples:



✅ Đăng nhập

❌ Dang nhap



✅ Đăng xuất

❌ Dang xuat



✅ Gửi câu hỏi

❌ Gui cau hoi



✅ Tóm tắt cốt truyện

❌ Tom tat cot truyen



✅ Quản lý tài liệu

❌ Quan ly tai lieu



All labels, buttons, placeholders, validation messages, notifications, dialog messages, menus and page titles displayed to users must be written in proper Vietnamese with full Unicode characters.



Do NOT use Vietnamese without diacritics anywhere in the UI unless the content comes directly from user input or uploaded documents.



All source files (.cshtml, .cs, resource files) must be saved using UTF-8 encoding to avoid character corruption.

## Language Consistency



The application language is Vietnamese.



Rules:



\- All UI text must be Vietnamese.

\- Do not mix English and Vietnamese in menus or buttons.

\- Internal class names, methods, variables and database fields remain in English.

\- Only the user-facing interface is Vietnamese.



Examples:



Controller:

DocumentController



Service:

DocumentService



Entity:

Document



UI:



"Tài liệu"



"Đăng nhập"



"Đăng xuất"



"Cập nhật"



"Tìm kiếm"



"Quản lý bộ môn"



"Quản lý môn học"



"Chi tiết tài liệu"



## Coding Standards & Architecture Rules (MANDATORY)

All team members MUST follow these rules when adding or modifying code.

### 1. Layered Architecture

The solution follows Presentation -> BLL -> DAL and dependencies MUST flow in this direction only.

* finalProject (Presentation) depends on BLL.
* BLL depends on DAL.
* DAL depends on EF Core only.
* BLL MUST NOT reference Microsoft.AspNetCore.*.
* DAL MUST NOT reference BLL or finalProject.
* Presentation MUST NOT reference DAL directly. Always go through BLL services.

### 2. Naming Conventions

* Class names, method names, property names, variable names and database fields stay in English.
* Only user-facing UI text (labels, buttons, messages, validation errors, dialog titles) is Vietnamese with full diacritics.
* File names match the primary type they contain (PascalCase, no abbreviations).
* DTOs end with `Dto`. Service interfaces start with `I`. Repositories implement the matching `I...Repository`.
* Entity names are singular nouns in PascalCase (e.g. `SubscriptionPackage`, not `SubscriptionPackages`).
* Enum values are PascalCase and stored as `int` columns with explicit `CheckConstraint`.

### 3. DTO and Entity Boundaries

* Never expose EF entities to the Presentation layer. Always map to DTOs inside the BLL service.
* DTOs live in `BLL/DTOs/`. ViewModels live in `finalProject/ViewModels/`.
* DTOs used for input from the Presentation layer MUST be marked with `[Required]`, `[StringLength]`, `[Range]` when applicable so ModelState validation works.
* ViewModels carry UI metadata (SelectListItem lists, formatted strings) and are mapped to DTOs inside controllers.
* Controllers MUST NOT receive EF entities as parameters.

### 4. Service and Repository Contracts

* Repository methods accept `CancellationToken cancellationToken = default` as the last parameter on every async method.
* Repository methods return `Task<IReadOnlyList<T>>` instead of `Task<List<T>>` for collections.
* Service interfaces are split by responsibility: `ISubscriptionPackageService` for package CRUD; `IPaymentService` for payment lifecycle; `IUserSubscriptionService` (when needed) for subscription lifecycle.
* Services depend on repository interfaces through constructor injection (no service locator, no static service access).
* One service method = one business transaction. If multiple repositories are involved, wrap them with `BeginTransactionAsync` inside the repository.
* Public methods on services MUST take `CurrentUserDto currentUser` as the first parameter and call `AuthorizationGuard.RequireRole` (or a domain-specific guard) before any data access.

### 5. Authorization Rules

* Authorization is enforced in BLL via `AuthorizationGuard`. Controllers only restrict access via `[Authorize(Roles = ...)]`.
* Roles:
  * `Admin` -> manages SubscriptionPackages, approves Payments, views all payments.
  * `Teacher` -> buys packages (creates payments), views own payments and own subscription.
  * `Student` -> buys packages (creates payments), views own payments and own subscription.
* Only `Admin` can mark a Payment as `Completed` or `Failed`. Teachers and Students cannot change payment status.

### 6. Database & Migration Rules

* Every table has `Id` as primary key, identity column.
* Use `nvarchar(n)` with explicit `HasMaxLength` for strings, `decimal(p,s)` for money, `datetime2` for dates.
* Money columns use `decimal(18,2)` and live on `Payment.Amount` and `SubscriptionPackage.Price`.
* Enum columns use `int` + `HasCheckConstraint("CK_X_Y", "[Y] IN (0, 1, ...)")`.
* Every FK relationship uses `OnDelete(DeleteBehavior.Restrict)` unless explicitly required otherwise.
* Every navigation property collection is initialised: `public ICollection<X> Xs { get; set; } = new List<X>();`
* Migrations follow the naming `YYYYMMDDHHMMSS_Stage<N><ShortDescription>.cs` and are generated with `dotnet ef migrations add`.
* The migration must include both `Up` and `Down`. Manual edits to migrations are allowed only to keep data safe.
* `AppDbContextModelSnapshot.cs` MUST be regenerated together with the migration.

### 7. UI Rules

* Every View sets `ViewData["Title"]` to a Vietnamese sentence with diacritics.
* Every form uses `asp-action` and `asp-controller`. Never hard-code URLs.
* Every form has `<partial name="_ValidationScriptsPartial" />` inside `@section Scripts`.
* Empty state messages must be Vietnamese: "Chưa có dữ liệu.", "Chưa có gói đăng ký nào.".
* Date and money formatting uses `.ToLocalTime().ToString("g")` and `string.Format(new CultureInfo("vi-VN"), "{0:C0}", value)` (or `ToString("N0")` + "đ").
* Status badges use Bootstrap `text-bg-success` (Completed / Active), `text-bg-warning` (Pending), `text-bg-secondary` (Inactive), `text-bg-danger` (Failed).
* Sidebar menu links in `_Layout.cshtml` are filtered by role with `@if (User.IsInRole(StudyMateRoles.X))`.

### 8. ViewModel Rules

* ViewModels are placed in `finalProject/ViewModels/`.
* A ViewModel used by a Create + Edit form must contain the same fields plus any SelectList lists needed by the view.
* `[Required]`, `[StringLength]`, `[Range]`, `[Display(Name = "...")]` annotations are mandatory. Display names are Vietnamese.
* ViewModels never reference EF entities.

### 9. Controller Rules

* Every POST action has `[HttpPost]` + `[ValidateAntiForgeryToken]`.
* Controllers call services with `await _service.MethodAsync(GetCurrentUser(), dto, cancellationToken)`.
* `GetCurrentUser()` reads `ClaimTypes.NameIdentifier` and `ClaimTypes.Role` from `User`.
* Controllers catch `InvalidOperationException` from services and add it to `ModelState` so the user sees a friendly Vietnamese message.
* Controllers never call repositories directly.

### 10. Unit Test Rules

* Tests live in `BLL.Tests/`.
* Tests use xUnit (`[Fact]`) and follow Arrange-Act-Assert.
* Repositories are replaced by `Fake*Repository` private sealed classes inside the test class. No EF Core / no DB.
* Services are constructed with the fake repositories and any needed fake collaborators.
* Every service must have at least: a happy-path test, an unauthorized-role test, and a not-found test.
* Test method names follow `MethodName_DoesWhat_WhenCondition`.

### 11. Git & Commit Rules

* One commit per logical change. Do not mix unrelated changes.
* Commit messages are short Vietnamese or English sentences describing the WHY, not the WHAT.
* Migrations are committed together with the entity changes that produced them.
* Never commit `bin/` or `obj/` (handled by `.gitignore`).

### 12. Stage 12 Scope (Subscription Packages & Payments)

This stage introduces three new tables: `SubscriptionPackages`, `Payments`, `UserSubscriptions`.

* `SubscriptionPackage` is the catalogue entry (name, description, price, duration in days, max AI tokens allowed, isActive). Admin manages it.
* `Payment` records a User's intent to buy a `SubscriptionPackage`. It has `Status` (`Pending` / `Completed` / `Failed`) and a snapshot of `Amount` at purchase time. It is created in `Pending` state.
* `UserSubscription` is granted to a User only when the related Payment moves to `Completed`. It has `StartDate`, `EndDate`, `RemainingTokens`, `IsActive`. Auto-deactivates when `EndDate < UtcNow`.
* A User can hold at most one active `UserSubscription`. Buying a new package while having an active subscription is allowed but the new subscription starts after the current one ends (no overlap).
* Soft delete is NOT applied to `SubscriptionPackage`; use `IsActive = false` instead.
* Hard delete is NOT applied to `Payment` or `UserSubscription`. Use status flags.
* Admin can view all payments (Manage screen). Teacher/Student can view only their own payments and their own subscription.

