# StudyMate AI - PROJECT_RULES

## Technology

-   ASP.NET Core MVC (.NET 10)
-   C#
-   EF Core
-   SQL Server
-   SignalR

## Architecture

Presentation -\> BLL -\> DAL

## Roles

### Admin

-   No Upload
-   Chunk Configuration
-   Department Management
-   Subject Management
-   View all Documents
-   AI Usage Dashboard

### Teacher

-   Upload only assigned Subjects
-   View/Edit only own Documents
-   Archive/Re-index own Documents

### Student

-   Read-only
-   View indexed documents

## Department Rule

One Department has only ONE Manager Teacher.

## Completed Stages

1.  Architecture
2.  Database
3.  Repository & BLL 3.5 Authentication
4.  UI Foundation
5.  Department & Subject
6.  Document List
7.  Upload Document

## Never Change

-   Architecture
-   Database schema
-   Authentication
-   Completed UI
-   Repository contracts

## Workflow

Read solution -\> Respect completed stages -\> Implement requested stage
-\> Build successfully.
