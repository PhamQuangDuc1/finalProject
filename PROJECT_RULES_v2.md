# PROJECT\_RULES.md

Completed through Stage 11.

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

