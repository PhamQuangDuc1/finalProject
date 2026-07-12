# PROJECT_RULES.md

Completed through Stage 11.

Technology: ASP.NET Core MVC, C#, EF Core, SQL Server, SignalR.

Architecture: Presentation -> BLL -> DAL.

Admin: manage system, no upload.
Teacher: upload/manage own documents only.
Student: read-only.

Business Rules:
- One Department has one Manager Teacher.
- Chunk settings only by Admin.
- Soft Delete for Archive.
- Authorization enforced in BLL.

Never change architecture, database schema, authentication, authorization, repository/service interfaces without request.
