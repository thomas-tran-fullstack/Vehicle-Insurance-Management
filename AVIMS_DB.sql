CREATE DATABASE AVIMS_DB;
GO
USE AVIMS_DB;
GO

/* ---------- MASTER/SECURITY ---------- */
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(150) NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    RoleId INT NOT NULL,
    IsLocked BIT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    BannedUntil DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    LastLoginAt DATETIME2 NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

CREATE TABLE AuditLogs (
    LogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(255) NOT NULL,
    Entity NVARCHAR(100) NULL,
    EntityId NVARCHAR(50) NULL,
    Meta NVARCHAR(MAX) NULL,
    LogDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* ---------- PEOPLE ---------- */
CREATE TABLE Staff (
    StaffId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Position NVARCHAR(100) NULL,
    Avatar NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    CustomerName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255) NULL,
    Phone NVARCHAR(20) NULL,
    Avatar NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO


CREATE TABLE Admins (
    AdminId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    AdminLevel INT NOT NULL DEFAULT 1,   -- 1=standard admin, 2=super admin
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* ---------- INSURANCE TYPES ---------- */
CREATE TABLE InsuranceTypes (
    InsuranceTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeCode NVARCHAR(30) NOT NULL UNIQUE,     -- e.g. CAR_BASIC, MOTO_PLUS
    TypeName NVARCHAR(150) NOT NULL,           -- display name
    Description NVARCHAR(500) NULL,
    BaseRatePercent DECIMAL(5,2) NOT NULL DEFAULT 2.50, -- base premium % of vehicle rate/value
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

/* ---------- VEHICLE ---------- */
CREATE TABLE Vehicles (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    VehicleName NVARCHAR(100) NOT NULL,        -- e.g. Toyota Vios / Honda Wave
    VehicleOwnerName NVARCHAR(150) NULL,       -- can differ from customer
    Make NVARCHAR(100) NULL,                   -- brand
    Model NVARCHAR(100) NULL,                  -- merged "Vehicle Model"
    VehicleVersion NVARCHAR(50) NULL,          -- version/trim
    VehicleRate DECIMAL(18,2) NOT NULL,        -- insured value / vehicle value
    BodyNumber NVARCHAR(100) NOT NULL,
    EngineNumber NVARCHAR(100) NOT NULL,
    VehicleNumber NVARCHAR(50) NOT NULL,       -- plate number
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    CONSTRAINT UQ_Vehicles_BodyNumber UNIQUE (BodyNumber),
    CONSTRAINT UQ_Vehicles_EngineNumber UNIQUE (EngineNumber),
    CONSTRAINT UQ_Vehicles_VehicleNumber UNIQUE (VehicleNumber)
);
GO

CREATE TABLE VehicleInspections (
    InspectionId INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    AssignedStaffId INT NULL,
    ScheduledDate DATETIME2 NULL,
    CompletedDate DATETIME2 NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'NEW',   -- NEW/SCHEDULED/COMPLETED/FAILED
    Result NVARCHAR(1000) NULL,
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (AssignedStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- ESTIMATE ---------- */
CREATE TABLE Estimates (
    EstimateId INT IDENTITY(1,1) PRIMARY KEY,
    EstimateNumber BIGINT NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    VehicleId INT NOT NULL,
    InsuranceTypeId INT NOT NULL,
    VehicleRate DECIMAL(18,2) NOT NULL,
    Warranty NVARCHAR(100) NULL,
    EstimatedPremium DECIMAL(18,2) NOT NULL,
    Notes NVARCHAR(500) NULL,
    CreatedByStaffId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (InsuranceTypeId) REFERENCES InsuranceTypes(InsuranceTypeId),
    FOREIGN KEY (CreatedByStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- POLICY ---------- */
CREATE TABLE Policies (
    PolicyId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyNumber BIGINT NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    VehicleId INT NOT NULL,
    InsuranceTypeId INT NOT NULL,
    PolicyStartDate DATE NOT NULL,
    PolicyEndDate DATE NOT NULL,
    DurationMonths INT NOT NULL,               -- duration in months
    Warranty NVARCHAR(100) NULL,
    AddressProofPath NVARCHAR(255) NULL,       -- file path/url
    PremiumAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'ACTIVE',  -- DRAFT/ACTIVE/CANCELLED/LAPSED
    IsHidden BIT NOT NULL DEFAULT 0,
    CreatedByStaffId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (InsuranceTypeId) REFERENCES InsuranceTypes(InsuranceTypeId),
    FOREIGN KEY (CreatedByStaffId) REFERENCES Staff(StaffId)
);
GO

CREATE TABLE PolicyDocuments (
    DocumentId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyId INT NOT NULL,
    DocType NVARCHAR(50) NOT NULL DEFAULT 'POLICY_PDF', -- POLICY_PDF/RECEIPT/ENDORSEMENT
    FilePath NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
GO

/* ---------- BILLING / PAYMENT ---------- */
CREATE TABLE Invoices (
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber BIGINT NOT NULL UNIQUE,
    PolicyId INT NOT NULL,
    IssueDate DATE NOT NULL,
    DueDate DATE NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'UNPAID', -- UNPAID/PAID/VOID
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
GO

CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Method NVARCHAR(30) NOT NULL,              -- CASH/CARD/BANK_TRANSFER/ONLINE
    TransactionRef NVARCHAR(100) NULL,
    PaidAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'SUCCESS', -- SUCCESS/FAILED/REFUNDED
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId)
);
GO

/* ---------- CLAIM ---------- */
CREATE TABLE Claims (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimNumber BIGINT NOT NULL UNIQUE,
    PolicyId INT NOT NULL,
    AccidentPlace NVARCHAR(255) NOT NULL,
    AccidentDate DATE NOT NULL,
    InsuredAmount DECIMAL(18,2) NOT NULL,
    ClaimableAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'SUBMITTED',  -- SUBMITTED/NEED_INFO/APPROVED/REJECTED/PAID
    SubmittedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReviewedByStaffId INT NULL,
    DecisionAt DATETIME2 NULL,
    DecisionNote NVARCHAR(500) NULL,
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId),
    FOREIGN KEY (ReviewedByStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- COMPANY EXPENSES ---------- */
CREATE TABLE CompanyExpenses (
    ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
    ExpenseDate DATE NOT NULL,
    ExpenseType NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount > 0)
);
GO

/* ---------- PUBLIC/CONTENT ---------- */
CREATE TABLE Feedback (
    FeedbackId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Rating INT NULL CHECK (Rating BETWEEN 1 AND 5),
    Content NVARCHAR(500) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'NEW', -- NEW/IN_PROGRESS/RESOLVED
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);
GO

CREATE TABLE Testimonials (
    TestimonialId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    Approved BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);
GO

CREATE TABLE Contacts (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NULL,
    Phone NVARCHAR(20) NULL,
    Subject NVARCHAR(200) NULL,
    Message NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE SupportArticles (
    ArticleId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsPublished BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    ToUserId INT NULL,                   -- NULL => broadcast
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Channel NVARCHAR(30) NOT NULL DEFAULT 'IN_APP', -- IN_APP/EMAIL/SMS
    Status NVARCHAR(20) NOT NULL DEFAULT 'QUEUED',  -- QUEUED/SENT/FAILED
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    SentAt DATETIME2 NULL,
    FOREIGN KEY (ToUserId) REFERENCES Users(UserId)
);
GO

/* ---------- AUTH SUPPORT ---------- */
CREATE TABLE PasswordResetOtps (
    OtpId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OtpCode NVARCHAR(10) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    UsedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================
   SAMPLE DATA
   NOTE: PasswordHash below are placeholders (bcrypt examples).
         Replace with hashes generated by your auth service.
   ========================================================= */

-- Roles
INSERT INTO Roles (RoleName) VALUES ('ADMIN'), ('STAFF'), ('CUSTOMER');
GO

-- Users (admin, staff, customers)
INSERT INTO Users (Username, PasswordHash, Email, Phone, RoleId, IsLocked)
VALUES
('admin','$2a$10$spGlapzTEfAerLa2lJ9TSO6dwKiV8reSE.0L0PkoY4W4csj7rpcqG', 'admin@avims.com',  '0900000001', 1, 0),
('staff','$2a$12$0wltvLEeIBOgEqg1v.yIEOHPHHmIWdrRESZ2cY7gdyiyUVErj6wJ6', 'inspector@avims.com','0900000002', 2, 0),
('customer','$2a$12$uEg4KMoCueGMfAkf1A5/iOvbzbfCr5wK4BBX0J0cHrZerBxsTQ39i', 'phamc@avims.com',    '0912345678', 3, 0),
('customer2','$2a$12$uEg4KMoCueGMfAkf1A5/iOvbzbfCr5wK4BBX0J0cHrZerBxsTQ39i', 'lethud@avims.com',   '0912345679', 3, 0),
('staff2','$2a$12$0wltvLEeIBOgEqg1v.yIEOHPHHmIWdrRESZ2cY7gdyiyUVErj6wJ6', 'inspector2@avims.com','0900000003', 2, 0);
GO


-- Admin row
INSERT INTO Admins (UserId, AdminLevel, IsActive)
VALUES
(1, 2, 1);
GO

-- Staff rows
INSERT INTO Staff (UserId, FullName, Phone, Position, Avatar)
VALUES
(2, 'Nguyen Van A', '0987654321', 'Inspector', '/images/staff-avatar-1.png'),
(5, 'Tran Thi B',   '0987654322', 'Claims Officer', '/images/staff-avatar-2.png');
GO

-- Customer rows
INSERT INTO Customers (UserId, CustomerName, Address, Phone, Avatar)
VALUES
(3, 'Pham Minh C', '123 Main St, Ho Chi Minh City', '0912345678', '/images/customer-avatar-1.png'),
(4, 'Le Thu D',    '456 Oak Ave, Hanoi',            '0912345679', '/images/customer-avatar-2.png');
GO

-- Insurance Types
INSERT INTO InsuranceTypes (TypeCode, TypeName, Description, BaseRatePercent, IsActive)
VALUES
('CAR_BASIC',   N'Car Insurance - Basic',   N'Basic coverage for private cars',        2.50, 1),
('CAR_PLUS',    N'Car Insurance - Plus',    N'Extended coverage + roadside support',  3.20, 1),
('MOTO_BASIC',  N'Motorbike Insurance',     N'Coverage for motorbikes',               1.80, 1),
('COMM_BASIC',  N'Commercial Vehicle',      N'Coverage for commercial vehicles',      3.80, 1);
GO

-- Vehicles
INSERT INTO Vehicles (CustomerId, VehicleName, VehicleOwnerName, Make, Model, VehicleVersion, VehicleRate, BodyNumber, EngineNumber, VehicleNumber)
VALUES
(1, N'Toyota Vios', N'Pham Minh C', N'Toyota', N'Vios', N'G 1.5', 450000000, 'BODY-VIOS-0001', 'ENG-VIOS-0001', '51A-123.45'),
(1, N'Honda Air Blade', N'Pham Minh C', N'Honda', N'Air Blade', N'150cc', 45000000, 'BODY-AB-0001', 'ENG-AB-0001', '59C1-888.88'),
(2, N'Ford Ranger', N'Le Thu D', N'Ford', N'Ranger', N'Wildtrak', 850000000, 'BODY-RANGER-0001', 'ENG-RANGER-0001', '30G-678.90');
GO

-- Vehicle Inspections
INSERT INTO VehicleInspections (VehicleId, AssignedStaffId, ScheduledDate, Status, Result)
VALUES
(1, 1, DATEADD(day, 1, SYSDATETIME()), 'SCHEDULED', NULL),
(3, 1, DATEADD(day, 2, SYSDATETIME()), 'SCHEDULED', NULL);
GO

-- Estimates (EstimateNumber: use big numbers for realism)
INSERT INTO Estimates (EstimateNumber, CustomerId, VehicleId, InsuranceTypeId, VehicleRate, Warranty, EstimatedPremium, Notes, CreatedByStaffId)
VALUES
(2026000001, 1, 1, 1, 450000000, N'12 months', 11250000, N'Basic car estimate', 1),
(2026000002, 1, 2, 3, 45000000,  N'6 months',   810000,  N'Motorbike estimate', 1),
(2026000003, 2, 3, 2, 850000000, N'12 months', 27200000, N'Plus package quote',  1);
GO

-- Policies
INSERT INTO Policies (PolicyNumber, CustomerId, VehicleId, InsuranceTypeId, PolicyStartDate, PolicyEndDate, DurationMonths, Warranty, AddressProofPath, PremiumAmount, Status, CreatedByStaffId)
VALUES
(2026100001, 1, 1, 1, '2026-01-01', '2026-12-31', 12, N'12 months', '/uploads/proofs/phamc_addr.pdf', 11250000, 'ACTIVE', 1),
(2026100002, 1, 2, 3, '2026-01-15', '2026-07-14', 6,  N'6 months',  '/uploads/proofs/phamc_addr.pdf',   810000, 'ACTIVE', 1),
(2026100003, 2, 3, 2, '2026-02-01', '2027-01-31', 12, N'12 months', '/uploads/proofs/lethud_addr.pdf', 27200000, 'ACTIVE', 1);
GO

-- Policy Documents
INSERT INTO PolicyDocuments (PolicyId, DocType, FilePath)
VALUES
(1, 'POLICY_PDF', '/docs/policies/2026100001.pdf'),
(2, 'POLICY_PDF', '/docs/policies/2026100002.pdf'),
(3, 'POLICY_PDF', '/docs/policies/2026100003.pdf');
GO

-- Invoices
INSERT INTO Invoices (InvoiceNumber, PolicyId, IssueDate, DueDate, Amount, Status, Notes)
VALUES
(2026200001, 1, '2026-01-01', '2026-01-07', 11250000, 'PAID',  N'Initial premium'),
(2026200002, 2, '2026-01-15', '2026-01-20',   810000, 'PAID',  N'Initial premium'),
(2026200003, 3, '2026-02-01', '2026-02-10', 27200000, 'UNPAID',N'Awaiting payment');
GO

-- Payments
INSERT INTO Payments (InvoiceId, Amount, Method, TransactionRef, PaidAt, Status)
VALUES
(1, 11250000, 'ONLINE', 'PAY-TRX-0001', '2026-01-01T10:15:00', 'SUCCESS'),
(2,   810000, 'CARD',   'PAY-TRX-0002', '2026-01-15T15:05:00', 'SUCCESS');
GO

-- Claims
INSERT INTO Claims (ClaimNumber, PolicyId, AccidentPlace, AccidentDate, InsuredAmount, ClaimableAmount, Status, ReviewedByStaffId, DecisionAt, DecisionNote)
VALUES
(2026300001, 1, N'District 1, HCMC', '2026-03-02', 450000000, 15000000, 'APPROVED', 1, '2026-03-10T09:00:00', N'Approved after document verification'),
(2026300002, 3, N'Cau Giay, Hanoi',  '2026-03-05', 850000000, 30000000, 'SUBMITTED', NULL, NULL, NULL);
GO

-- Company Expenses
INSERT INTO CompanyExpenses (ExpenseDate, ExpenseType, Amount)
VALUES
('2026-01-05', N'Office Rent', 15000000),
('2026-01-10', N'Utilities',    2500000),
('2026-01-20', N'Staff Salary', 35000000),
('2026-02-05', N'Marketing',     5000000);
GO

-- Feedback
INSERT INTO Feedback (CustomerId, Rating, Content, Status)
VALUES
(1, 5, N'Service was fast and clear. Great!', 'RESOLVED'),
(2, 4, N'Website is easy to use, but payment page could be improved.', 'NEW');
GO

-- Testimonials
INSERT INTO Testimonials (CustomerId, Content, Approved)
VALUES
(1, N'AutoSure helped me buy insurance quickly online.', 1),
(2, N'Friendly staff and clear process. Recommended!', 0);
GO

-- Contacts (Contact Us)
INSERT INTO Contacts (Name, Email, Phone, Subject, Message)
VALUES
(N'Visitor X', 'visitor@example.com', '0909999999', N'Ask about policy types', N'Please advise which insurance suits a commercial vehicle.');
GO

-- Support Articles (FAQ)
INSERT INTO SupportArticles (Title, Content, IsPublished)
VALUES
(N'How to buy a policy online?', N'Steps: Login -> Buy Policy -> Fill form -> Pay -> Download policy.', 1),
(N'How to initiate a claim?',    N'Open Claims -> New Claim -> Fill details -> Upload evidence -> Submit.', 1);
GO

-- Notifications
INSERT INTO Notifications (ToUserId, Title, Message, Channel, Status, SentAt)
VALUES
(3, N'Renewal reminder', N'Your policy 2026100001 will expire soon. Please renew before end date.', 'IN_APP', 'SENT', SYSDATETIME()),
(NULL, N'System maintenance', N'The portal will be maintained on Sunday 01:00-03:00.', 'IN_APP', 'QUEUED', NULL);
GO
