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

/* ---------- BRANCHES ---------- */
CREATE TABLE Branches (
    BranchId INT IDENTITY(1,1) PRIMARY KEY,
    BranchName NVARCHAR(150) NOT NULL,
    ManagerName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Hotline NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    OperatingStartTime TIME NOT NULL,
    OperatingEndTime TIME NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedDate DATETIME2 NULL
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

/* ---------- VEHICLE MODELS ---------- */
CREATE TABLE VehicleModels (
    ModelId INT IDENTITY(1,1) PRIMARY KEY,
    ModelName NVARCHAR(100) NULL,
    VehicleClass NVARCHAR(50) NULL,
    VehicleType NVARCHAR(50) NULL,                  -- Car / Motorbike / Truck
    Description NVARCHAR(255) NULL
);
GO

/* ---------- VEHICLE ---------- */
CREATE TABLE Vehicles (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    ModelId INT NULL,
    VehicleName NVARCHAR(100) NOT NULL,
    VehicleType NVARCHAR(MAX) NULL,
    VehicleBrand NVARCHAR(MAX) NULL,
    VehicleSegment NVARCHAR(MAX) NULL,
    VehicleVersion NVARCHAR(50) NULL,
    VehicleRate DECIMAL(18,2) NULL,
    BodyNumber NVARCHAR(100) NOT NULL,
    EngineNumber NVARCHAR(100) NOT NULL,
    VehicleNumber NVARCHAR(50) NOT NULL,
    RegistrationDate DATETIME2 NULL,
    SeatCount INT NULL,
    VehicleImage NVARCHAR(MAX) NULL,
    ManufactureYear INT NULL,
    CreatedDate DATETIME2 NULL,
    UpdatedDate DATETIME2 NULL,
    VehicleOwnerName NVARCHAR(150) NULL,
    VehicleModelName NVARCHAR(100) NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (ModelId) REFERENCES VehicleModels(ModelId),
    CONSTRAINT UQ_Vehicles_BodyNumber UNIQUE (BodyNumber),
    CONSTRAINT UQ_Vehicles_EngineNumber UNIQUE (EngineNumber),
    CONSTRAINT UQ_Vehicles_VehicleNumber UNIQUE (VehicleNumber)
);
GO

/* ---------- ESTIMATE ---------- */
CREATE TABLE Estimates (
    EstimateId INT IDENTITY(1,1) PRIMARY KEY,
    EstimateNumber BIGINT NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    CustomerNameSnapshot NVARCHAR(150) NULL,
    CustomerPhoneSnapshot NVARCHAR(20) NULL,
    VehicleId INT NOT NULL,
    VehicleNameSnapshot NVARCHAR(100) NULL,
    VehicleModelSnapshot NVARCHAR(100) NULL,
    InsuranceTypeId INT NOT NULL,
    PolicyTypeSnapshot NVARCHAR(30) NULL,
    VehicleRate DECIMAL(18,2) NOT NULL,
    Warranty NVARCHAR(100) NULL,
    BasePremium DECIMAL(18,2) NULL,
    Surcharge DECIMAL(18,2) NULL,
    TaxAmount DECIMAL(18,2) NULL,
    EstimatedPremium DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'DRAFT', -- DRAFT/SUBMITTED/APPROVED/REJECTED/CONVERTED
    ValidUntil DATETIME2 NULL,
    Notes NVARCHAR(500) NULL,
    CreatedByStaffId INT NULL,
    ApprovedByStaffId INT NULL,
    DecisionAt DATETIME2 NULL,
    DecisionNote NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (InsuranceTypeId) REFERENCES InsuranceTypes(InsuranceTypeId),
    FOREIGN KEY (CreatedByStaffId) REFERENCES Staff(StaffId),
    FOREIGN KEY (ApprovedByStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- POLICY ---------- */
CREATE TABLE Policies (
    PolicyId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyNumber BIGINT NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    CustomerNameSnapshot NVARCHAR(150) NULL,
    CustomerAddressSnapshot NVARCHAR(255) NULL,
    CustomerPhoneSnapshot NVARCHAR(20) NULL,
    VehicleId INT NOT NULL,
    VehicleNumberSnapshot NVARCHAR(50) NULL,
    VehicleNameSnapshot NVARCHAR(100) NULL,
    VehicleModelSnapshot NVARCHAR(100) NULL,
    VehicleVersionSnapshot NVARCHAR(50) NULL,
    VehicleRateSnapshot DECIMAL(18,2) NULL,
    VehicleWarrantySnapshot NVARCHAR(100) NULL,
    VehicleBodyNumberSnapshot NVARCHAR(100) NULL,
    VehicleEngineNumberSnapshot NVARCHAR(100) NULL,
    InsuranceTypeId INT NOT NULL,
    PolicyTypeSnapshot NVARCHAR(30) NULL,
    PolicyStartDate DATE NOT NULL,
    PolicyEndDate DATE NOT NULL,
    DurationMonths INT NOT NULL,               -- duration in months
    Warranty NVARCHAR(100) NULL,
    AddressProofPath NVARCHAR(255) NULL,       -- file path/url
    PaymentDueDate DATE NULL,
    PremiumAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'WAITING_PAYMENT',  -- DRAFT/WAITING_PAYMENT/ACTIVE/CANCELLED/LAPSED
    PendingRenewalMonths INT NULL,
    PendingRenewalStartDate DATE NULL,
    PendingRenewalEndDate DATE NULL,
    CancelEffectiveDate DATE NULL,
    CancellationReason NVARCHAR(500) NULL,
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

/* ---------- BILLS ---------- */
CREATE TABLE Bills (
    BillId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyId INT NOT NULL,
    BillDate DATE NOT NULL,
    DueDate DATE NULL,
    BillType NVARCHAR(20) NOT NULL DEFAULT 'INITIAL',  -- INITIAL/RENEWAL/ADDITIONAL
    Amount DECIMAL(18,2) NOT NULL,
    Paid BIT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'UNPAID',    -- UNPAID/PAID
    PaidAt DATETIME2 NULL,
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
    CustomerNameSnapshot NVARCHAR(150) NULL,
    PolicyStartDateSnapshot DATE NULL,
    PolicyEndDateSnapshot DATE NULL,
    AccidentPlace NVARCHAR(255) NOT NULL,
    AccidentDate DATE NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DocumentPath NVARCHAR(255) NULL,
    InsuredAmount DECIMAL(18,2) NOT NULL,
    ClaimableAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'SUBMITTED',  -- SUBMITTED/UNDER_REVIEW/REQUEST_MORE_INFO/APPROVED/REJECTED/PAID
    SubmittedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReviewedByStaffId INT NULL,
    ReviewedAt DATETIME2 NULL,
    ReviewNote NVARCHAR(500) NULL,
    ApprovedByStaffId INT NULL,
    DecisionAt DATETIME2 NULL,
    DecisionNote NVARCHAR(500) NULL,
    PaidAt DATETIME2 NULL,
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId),
    FOREIGN KEY (ReviewedByStaffId) REFERENCES Staff(StaffId),
    FOREIGN KEY (ApprovedByStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- VEHICLE INSPECTIONS ---------- */
CREATE TABLE VehicleInspections (
    InspectionId INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    ClaimId INT NULL,
    AssignedStaffId INT NULL,
    ScheduledDate DATETIME2 NULL,
    InspectionLocation NVARCHAR(255) NULL,
    CompletedDate DATETIME2 NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'NEW',   -- NEW/SCHEDULED/IN_PROGRESS/COMPLETED/VERIFIED/FAILED
    OverallAssessment NVARCHAR(MAX) NULL,
    ConfirmedCorrect BIT NULL,
    DocumentPath NVARCHAR(255) NULL,
    Result NVARCHAR(1000) NULL,
    VerifiedByStaffId INT NULL,
    VerifiedAt DATETIME2 NULL,
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId),
    FOREIGN KEY (AssignedStaffId) REFERENCES Staff(StaffId),
    FOREIGN KEY (VerifiedByStaffId) REFERENCES Staff(StaffId)
);
GO

/* ---------- COMPANY EXPENSES ---------- */
CREATE TABLE CompanyExpenses (
    ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
    ExpenseDate DATE NOT NULL,
    ExpenseType NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount > 0)
);
GO

/* ---------- INSURANCE CANCELLATION ---------- */
CREATE TABLE InsuranceCancellations (
    CancellationId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyId INT NOT NULL,
    CancelDate DATE NOT NULL,
    RefundAmount DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
GO

/* ---------- PENALTIES ---------- */
CREATE TABLE Penalties (
    PenaltyId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyId INT NOT NULL,
    PenaltyDate DATE NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
GO

/* ---------- PUBLIC/CONTENT ---------- */
CREATE TABLE Feedback (
    FeedbackId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Rating INT NULL CHECK (Rating BETWEEN 1 AND 5),
    Content NVARCHAR(500) NULL,
    IsPinned BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);
GO

CREATE TABLE Testimonials (
    TestimonialId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Content NVARCHAR(500) NULL,
    Rating INT NULL CHECK (Rating BETWEEN 1 AND 5),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- Published, Pending, Denied
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);
GO

CREATE TABLE ContactCategories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Contacts (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NULL,
    Phone NVARCHAR(20) NULL,
    Subject NVARCHAR(200) NULL,
    Message NVARCHAR(1000) NOT NULL,
    CategoryId INT NULL,
    UserId INT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Open',
    FOREIGN KEY (CategoryId) REFERENCES ContactCategories(CategoryId)
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

CREATE TABLE Faqs (
    FaqId INT IDENTITY(1,1) PRIMARY KEY,
    Question NVARCHAR(500) NOT NULL,
    Answer NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL
);
GO

CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,                     -- NULL => broadcast
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Type NVARCHAR(50) NULL,              -- RENEWAL_REMINDER, PAYMENT_DUE, etc
    Channel NVARCHAR(30) NOT NULL DEFAULT 'IN_APP', -- IN_APP/EMAIL/SMS
    Status NVARCHAR(20) NOT NULL DEFAULT 'QUEUED',  -- QUEUED/SENT/FAILED
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    SentAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
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

-- Vehicle Models
INSERT INTO VehicleModels (ModelName, VehicleClass, VehicleType, Description)
VALUES
(N'Vios', N'Sedan', N'Car', N'Toyota Vios compact sedan'),
(N'Air Blade', N'Scooter', N'Motorbike', N'Honda Air Blade 150cc'),
(N'Ranger', N'Pickup', N'Truck', N'Ford Ranger pickup truck'),
(N'Vision', N'Scooter', N'Motorbike', N'Honda Vision 110cc'),
(N'Wave Alpha', N'Underbone', N'Motorbike', N'Honda Wave Alpha 110cc'),
(N'Innova', N'MPV', N'Car', N'Toyota Innova 7 seater'),
(N'Kia Morning', N'Hatchback', N'Car', N'Kia Morning compact hatchback');
GO

-- Vehicles
INSERT INTO Vehicles (CustomerId, ModelId, VehicleName, VehicleType, VehicleBrand, VehicleSegment, VehicleVersion, VehicleRate, BodyNumber, EngineNumber, VehicleNumber, RegistrationDate, SeatCount, ManufactureYear, VehicleOwnerName, CreatedDate, UpdatedDate)
VALUES
(1, 1, N'Toyota Vios', N'Sedan', N'Toyota', N'Compact', N'G 1.5', 450000000, 'BODY-VIOS-0001', 'ENG-VIOS-0001', '51A-123.45', '2020-05-15', 5, 2020, N'Pham Minh C', SYSDATETIME(), SYSDATETIME()),
(1, 2, N'Honda Air Blade', N'Motorbike', N'Honda', N'Scooter', N'150cc', 45000000, 'BODY-AB-0001', 'ENG-AB-0001', '59C1-888.88', '2019-03-20', 2, 2019, N'Pham Minh C', SYSDATETIME(), SYSDATETIME()),
(2, 3, N'Ford Ranger', N'Pickup', N'Ford', N'Truck', N'Wildtrak', 850000000, 'BODY-RANGER-0001', 'ENG-RANGER-0001', '30G-678.90', '2021-08-10', 5, 2021, N'Le Thu D', SYSDATETIME(), SYSDATETIME());
GO

-- Vehicle Inspections
INSERT INTO VehicleInspections (VehicleId, AssignedStaffId, ScheduledDate, Status, Result)
VALUES
(1, 1, DATEADD(day, 1, SYSDATETIME()), 'SCHEDULED', NULL),
(3, 1, DATEADD(day, 2, SYSDATETIME()), 'SCHEDULED', NULL);
GO

-- Estimates (EstimateNumber: use big numbers for realism)
INSERT INTO Estimates (EstimateNumber, CustomerId, CustomerNameSnapshot, CustomerPhoneSnapshot, VehicleId, VehicleNameSnapshot, VehicleModelSnapshot, InsuranceTypeId, PolicyTypeSnapshot, VehicleRate, Warranty, BasePremium, Surcharge, TaxAmount, EstimatedPremium, Status, ValidUntil, Notes, CreatedByStaffId)
VALUES
(2026000001, 1, N'Pham Minh C', '0912345678', 1, N'Toyota Vios', N'Vios', 1, 'CAR_BASIC', 450000000, N'12 months', 11250000, 0, 1125000, 12375000, 'SUBMITTED', DATEADD(day, 7, SYSDATETIME()), N'Basic car estimate', 1),
(2026000002, 1, N'Pham Minh C', '0912345678', 2, N'Honda Air Blade', N'Air Blade', 3, 'MOTO_BASIC', 45000000, N'6 months', 405000, 0, 40500, 445500, 'APPROVED', DATEADD(day, 7, SYSDATETIME()), N'Motorbike estimate', 1),
(2026000003, 2, N'Le Thu D', '0912345679', 3, N'Ford Ranger', N'Ranger', 2, 'CAR_PLUS', 850000000, N'12 months', 27200000, 5440000, 3264000, 35904000, 'SUBMITTED', DATEADD(day, 7, SYSDATETIME()), N'Plus package quote', 1);
GO

-- Policies
INSERT INTO Policies (PolicyNumber, CustomerId, CustomerNameSnapshot, CustomerAddressSnapshot, CustomerPhoneSnapshot, VehicleId, VehicleNumberSnapshot, VehicleNameSnapshot, VehicleModelSnapshot, VehicleVersionSnapshot, VehicleRateSnapshot, VehicleWarrantySnapshot, VehicleBodyNumberSnapshot, VehicleEngineNumberSnapshot, InsuranceTypeId, PolicyTypeSnapshot, PolicyStartDate, PolicyEndDate, DurationMonths, Warranty, AddressProofPath, PaymentDueDate, PremiumAmount, Status, CreatedByStaffId)
VALUES
(2026100001, 1, N'Pham Minh C', N'123 Main St, Ho Chi Minh City', '0912345678', 1, '51A-123.45', N'Toyota Vios', N'Vios', N'G 1.5', 450000000, N'12 months', 'BODY-VIOS-0001', 'ENG-VIOS-0001', 1, 'CAR_BASIC', '2026-01-01', '2026-12-31', 12, N'12 months', '/uploads/proofs/phamc_addr.pdf', '2026-01-07', 12375000, 'ACTIVE', 1),
(2026100002, 1, N'Pham Minh C', N'123 Main St, Ho Chi Minh City', '0912345678', 2, '59C1-888.88', N'Honda Air Blade', N'Air Blade', N'150cc', 45000000, N'6 months', 'BODY-AB-0001', 'ENG-AB-0001', 3, 'MOTO_BASIC', '2026-01-15', '2026-07-14', 6, N'6 months', '/uploads/proofs/phamc_addr.pdf', '2026-01-20', 445500, 'ACTIVE', 1),
(2026100003, 2, N'Le Thu D', N'456 Oak Ave, Hanoi', '0912345679', 3, '30G-678.90', N'Ford Ranger', N'Ranger', N'Wildtrak', 850000000, N'12 months', 'BODY-RANGER-0001', 'ENG-RANGER-0001', 2, 'CAR_PLUS', '2026-02-01', '2027-01-31', 12, N'12 months', '/uploads/proofs/lethud_addr.pdf', '2026-02-10', 35904000, 'ACTIVE', 1);
GO

-- Bills
INSERT INTO Bills (PolicyId, BillDate, DueDate, BillType, Amount, Paid, Status, PaidAt)
VALUES
(1, '2026-01-01', '2026-01-07', 'INITIAL', 12375000, 1, 'PAID', '2026-01-01T10:15:00'),
(2, '2026-01-15', '2026-01-20', 'INITIAL', 445500, 1, 'PAID', '2026-01-15T15:05:00'),
(3, '2026-02-01', '2026-02-10', 'INITIAL', 35904000, 0, 'UNPAID', NULL);
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

-- Contact Categories
INSERT INTO ContactCategories (CategoryName, Description, IsActive)
VALUES
(N'General Inquiry', N'General questions about our services', 1),
(N'New Quote', N'Request for a new insurance quote', 1),
(N'Claim Support', N'Support with an existing claim', 1),
(N'Policy Update', N'Updates or changes to existing policy', 1),
(N'Feedback', N'General feedback or suggestions', 1);
GO

-- Company Expenses
INSERT INTO CompanyExpenses (ExpenseDate, ExpenseType, Description, Amount)
VALUES
('2026-01-05', N'Office Rent', N'Monthly office rent for head office', 15000000),
('2026-01-10', N'Utilities', N'Electricity and water bills', 2500000),
('2026-01-20', N'Staff Salary', N'Monthly payroll for staff', 35000000),
('2026-02-05', N'Marketing', N'Digital marketing campaign', 5000000);
GO

-- Feedback
INSERT INTO Feedback (CustomerId, Rating, Content, IsPinned, CreatedDate)
VALUES
(1, 5, N'Service was fast and clear. Great!', 1, GETDATE()),
(2, 4, N'Website is easy to use, but payment page could be improved.', 0, GETDATE());
GO

-- Testimonials
INSERT INTO Testimonials (CustomerId, Content, Rating, Status, CreatedDate)
VALUES
(1, N'AutoSure helped me buy insurance quickly online.', 5, 'Published', GETDATE()),
(2, N'Friendly staff and clear process. Recommended!', 4, 'Pending', GETDATE());
GO

-- Branches
INSERT INTO Branches (BranchName, ManagerName, Address, Hotline, Email, OperatingStartTime, OperatingEndTime, IsActive)
VALUES
(N'London Head Office', N'Sarah Jenkins', N'123 Financial District, London, EC2V 7NQ', '+44 20 7123 4567', 'london@avims.com', '08:00', '20:00', 1),
(N'Manchester Branch', N'Mark Thompson', N'45 Deansgate, Manchester', '+44 161 832 1234', 'manchester@avims.com', '09:00', '18:00', 1),
(N'Birmingham Branch', N'Elena Rodriguez', N'88 Colmore Row, Birmingham', '+44 121 236 5678', 'birmingham@avims.com', '09:00', '18:00', 1),
(N'Leeds Branch', N'James Morrison', N'123 City Square, Leeds', '+44 113 200 0001', 'leeds@avims.com', '08:30', '17:30', 1),
(N'Bristol Branch', N'Emma Wilson', N'45 Corn Street, Bristol', '+44 117 927 2000', 'bristol@avims.com', '09:00', '18:00', 1);
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
INSERT INTO Notifications (UserId, Title, Message, Channel, Status, SentAt)
VALUES
(3, N'Renewal reminder', N'Your policy 2026100001 will expire soon. Please renew before end date.', 'IN_APP', 'SENT', SYSDATETIME()),
(NULL, N'System maintenance', N'The portal will be maintained on Sunday 01:00-03:00.', 'IN_APP', 'QUEUED', NULL);
GO
