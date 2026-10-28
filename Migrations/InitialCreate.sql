CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "FullName" TEXT NOT NULL,
    "UserRole" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "AuditLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NULL,
    "UserName" TEXT NULL,
    "UserRole" TEXT NULL,
    "Action" TEXT NOT NULL,
    "EntityName" TEXT NULL,
    "EntityId" TEXT NULL,
    "Details" TEXT NULL,
    "TimestampUtc" TEXT NOT NULL,
    "IpAddress" TEXT NULL
);

CREATE TABLE "Departments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Departments" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "Description" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);

CREATE TABLE "Semesters" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Semesters" PRIMARY KEY AUTOINCREMENT,
    "SemesterNumber" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "AcademicYear" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "FacultyProfiles" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_FacultyProfiles" PRIMARY KEY AUTOINCREMENT,
    "ApplicationUserId" TEXT NULL,
    "EmployeeCode" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "DepartmentId" INTEGER NOT NULL,
    "Designation" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_FacultyProfiles_AspNetUsers_ApplicationUserId" FOREIGN KEY ("ApplicationUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FacultyProfiles_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Students" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
    "ApplicationUserId" TEXT NULL,
    "EnrollmentNumber" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "DepartmentId" INTEGER NOT NULL,
    "SemesterId" INTEGER NOT NULL,
    "EnrollmentYear" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Students_AspNetUsers_ApplicationUserId" FOREIGN KEY ("ApplicationUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Students_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Students_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Subjects" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Subjects" PRIMARY KEY AUTOINCREMENT,
    "SubjectCode" TEXT NOT NULL,
    "SubjectName" TEXT NOT NULL,
    "DepartmentId" INTEGER NOT NULL,
    "SemesterId" INTEGER NOT NULL,
    "Credits" INTEGER NOT NULL,
    "MaxInternalMarks" decimal(6,2) NOT NULL,
    "PassingMarks" decimal(6,2) NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Subjects_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Subjects_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AssessmentComponents" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AssessmentComponents" PRIMARY KEY AUTOINCREMENT,
    "SubjectId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "MaxMarks" decimal(6,2) NOT NULL,
    "WeightPercentage" decimal(6,2) NOT NULL,
    "DisplayOrder" INTEGER NOT NULL,
    CONSTRAINT "FK_AssessmentComponents_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE CASCADE
);

CREATE TABLE "FacultySubjectAssignments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_FacultySubjectAssignments" PRIMARY KEY AUTOINCREMENT,
    "FacultyProfileId" INTEGER NOT NULL,
    "SubjectId" INTEGER NOT NULL,
    "SemesterId" INTEGER NOT NULL,
    "AcademicYear" TEXT NOT NULL,
    "AssignedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_FacultySubjectAssignments_FacultyProfiles_FacultyProfileId" FOREIGN KEY ("FacultyProfileId") REFERENCES "FacultyProfiles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FacultySubjectAssignments_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FacultySubjectAssignments_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "InternalMarks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InternalMarks" PRIMARY KEY AUTOINCREMENT,
    "StudentId" INTEGER NOT NULL,
    "SubjectId" INTEGER NOT NULL,
    "SemesterId" INTEGER NOT NULL,
    "AcademicYear" TEXT NOT NULL,
    "CalculatedTotal" decimal(6,2) NOT NULL,
    "MaxTotal" decimal(6,2) NOT NULL,
    "Percentage" decimal(5,2) NOT NULL,
    "Grade" TEXT NOT NULL,
    "IsPass" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "SubmittedByUserId" TEXT NULL,
    "SubmittedAtUtc" TEXT NULL,
    "ApprovedByUserId" TEXT NULL,
    "ApprovedAtUtc" TEXT NULL,
    "PublishedByUserId" TEXT NULL,
    "PublishedAtUtc" TEXT NULL,
    "RejectionReason" TEXT NULL,
    "Remarks" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_InternalMarks_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InternalMarks_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InternalMarks_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ResultPublicationRecords" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ResultPublicationRecords" PRIMARY KEY AUTOINCREMENT,
    "SubjectId" INTEGER NOT NULL,
    "SemesterId" INTEGER NOT NULL,
    "AcademicYear" TEXT NOT NULL,
    "PublishedByUserId" TEXT NOT NULL,
    "PublishedAtUtc" TEXT NOT NULL,
    "TotalStudentsCount" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_ResultPublicationRecords_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ResultPublicationRecords_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "InternalMarkDetails" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InternalMarkDetails" PRIMARY KEY AUTOINCREMENT,
    "InternalMarkId" INTEGER NOT NULL,
    "AssessmentComponentId" INTEGER NOT NULL,
    "MarksObtained" decimal(6,2) NOT NULL,
    CONSTRAINT "FK_InternalMarkDetails_AssessmentComponents_AssessmentComponentId" FOREIGN KEY ("AssessmentComponentId") REFERENCES "AssessmentComponents" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InternalMarkDetails_InternalMarks_InternalMarkId" FOREIGN KEY ("InternalMarkId") REFERENCES "InternalMarks" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE INDEX "IX_AssessmentComponents_SubjectId" ON "AssessmentComponents" ("SubjectId");

CREATE UNIQUE INDEX "IX_Departments_Code" ON "Departments" ("Code");

CREATE UNIQUE INDEX "IX_FacultyProfiles_ApplicationUserId" ON "FacultyProfiles" ("ApplicationUserId");

CREATE INDEX "IX_FacultyProfiles_DepartmentId" ON "FacultyProfiles" ("DepartmentId");

CREATE UNIQUE INDEX "IX_FacultyProfiles_Email" ON "FacultyProfiles" ("Email");

CREATE UNIQUE INDEX "IX_FacultyProfiles_EmployeeCode" ON "FacultyProfiles" ("EmployeeCode");

CREATE UNIQUE INDEX "IX_FacultySubjectAssignments_FacultyProfileId_SubjectId_SemesterId_AcademicYear" ON "FacultySubjectAssignments" ("FacultyProfileId", "SubjectId", "SemesterId", "AcademicYear");

CREATE INDEX "IX_FacultySubjectAssignments_SemesterId" ON "FacultySubjectAssignments" ("SemesterId");

CREATE INDEX "IX_FacultySubjectAssignments_SubjectId" ON "FacultySubjectAssignments" ("SubjectId");

CREATE INDEX "IX_InternalMarkDetails_AssessmentComponentId" ON "InternalMarkDetails" ("AssessmentComponentId");

CREATE UNIQUE INDEX "IX_InternalMarkDetails_InternalMarkId_AssessmentComponentId" ON "InternalMarkDetails" ("InternalMarkId", "AssessmentComponentId");

CREATE INDEX "IX_InternalMarks_SemesterId" ON "InternalMarks" ("SemesterId");

CREATE UNIQUE INDEX "IX_InternalMarks_StudentId_SubjectId_SemesterId_AcademicYear" ON "InternalMarks" ("StudentId", "SubjectId", "SemesterId", "AcademicYear");

CREATE INDEX "IX_InternalMarks_SubjectId" ON "InternalMarks" ("SubjectId");

CREATE INDEX "IX_ResultPublicationRecords_SemesterId" ON "ResultPublicationRecords" ("SemesterId");

CREATE INDEX "IX_ResultPublicationRecords_SubjectId" ON "ResultPublicationRecords" ("SubjectId");

CREATE INDEX "IX_Semesters_SemesterNumber_AcademicYear" ON "Semesters" ("SemesterNumber", "AcademicYear");

CREATE UNIQUE INDEX "IX_Students_ApplicationUserId" ON "Students" ("ApplicationUserId");

CREATE INDEX "IX_Students_DepartmentId" ON "Students" ("DepartmentId");

CREATE UNIQUE INDEX "IX_Students_Email" ON "Students" ("Email");

CREATE UNIQUE INDEX "IX_Students_EnrollmentNumber" ON "Students" ("EnrollmentNumber");

CREATE INDEX "IX_Students_SemesterId" ON "Students" ("SemesterId");

CREATE INDEX "IX_Subjects_DepartmentId" ON "Subjects" ("DepartmentId");

CREATE INDEX "IX_Subjects_SemesterId" ON "Subjects" ("SemesterId");

CREATE UNIQUE INDEX "IX_Subjects_SubjectCode" ON "Subjects" ("SubjectCode");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261027123303_InitialCreate', '9.0.2');

COMMIT;

