# PROJECT_MEMORY.md

# StudyMate AI - Project Memory

> This file is the long-term memory for the project. Every time a new
> Codex/AI session starts:
>
> 1.  Read PROJECT_RULES.md
> 2.  Read PROJECT_MEMORY.md
> 3.  Read the entire solution
> 4.  Continue from the latest completed stage only.

------------------------------------------------------------------------

# Project Information

**Project Name:** StudyMate AI

**Course:** PRN222 Final Project

**Architecture**

Presentation (ASP.NET Core MVC) ↓ BLL (Business Logic Layer) ↓ DAL (Data
Access Layer)

Technology Stack

-   ASP.NET Core MVC (.NET 10)
-   C#
-   SQL Server
-   Entity Framework Core
-   SignalR
-   MVC Views (.cshtml)

Do NOT convert the project to: - Razor Pages - React - Vue - Angular

------------------------------------------------------------------------

# Current Progress

Completed:

-   Stage 1 - Architecture
-   Stage 2 - Database
-   Stage 3 - Repository & Business Layer
-   Stage 3.5 - Authentication & Authorization
-   Stage 4 - UI Foundation
-   Stage 5 - Department & Subject Management
-   Stage 6 - Document List
-   Stage 7 - Teacher Upload Document

Current Target:

Stage 8 (Document Details + Archive + Re-index)

------------------------------------------------------------------------

# Roles

## Admin

Can:

-   View all documents
-   Manage Departments
-   Manage Subjects
-   Assign Department Manager
-   Configure Chunk Settings
-   View AI Usage Dashboard

Cannot:

-   Upload learning documents
-   Edit teacher documents
-   Archive teacher documents
-   Re-index teacher documents

------------------------------------------------------------------------

## Teacher

Can:

-   Upload documents
-   Edit own documents
-   Archive own documents
-   Re-index own documents

Cannot:

-   View Teacher B documents
-   Edit Teacher B documents
-   Upload outside assigned Subjects

------------------------------------------------------------------------

## Student

Can:

-   View active indexed documents
-   Chat with AI
-   Read only

Cannot:

-   Upload
-   Edit
-   Archive
-   Configure system

------------------------------------------------------------------------

# Business Rules

-   One Department has only ONE Manager Teacher.
-   One Subject belongs to one Department.
-   Teacher uploads only to assigned Subjects.
-   Admin configures Chunk Settings.
-   Chunk configuration must NEVER be hard-coded.
-   Teacher only manages own uploaded documents.
-   Student only accesses active indexed documents.

------------------------------------------------------------------------

# SignalR Rules

Admin: - Receive all document update notifications.

Teacher: - Receive only notifications related to their own documents.

------------------------------------------------------------------------

# Never Change Without Explicit Request

-   Architecture
-   Database schema
-   Authentication
-   Authorization
-   Completed UI
-   Repository contracts
-   Service interfaces

------------------------------------------------------------------------

# Before Every New Task

1.  Read PROJECT_RULES.md
2.  Read PROJECT_MEMORY.md
3.  Read the whole solution.
4.  Do NOT redesign completed stages.
5.  Implement only the requested stage.
6.  Build successfully before finishing.
