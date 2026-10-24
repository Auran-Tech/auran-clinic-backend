SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @clinicEmail nvarchar(256) = N'$(ClinicEmail)';
DECLARE @localDate date = TRY_CONVERT(date, '$(LocalDate)', 23);
DECLARE @clinicId uniqueidentifier;

IF @localDate IS NULL
    THROW 51000, 'A valid clinic-local date is required.', 1;

SELECT @clinicId = clinic.Id
FROM Clinics AS clinic
JOIN Users AS adminUser ON adminUser.ClinicId = clinic.Id
WHERE adminUser.Email = @clinicEmail
  AND adminUser.IsSuperUser = 1
  AND clinic.TimeZoneId = 'Africa/Cairo';

IF @clinicId IS NULL
    THROW 51001, 'A Cairo-timezone local demo clinic administrator was not found.', 1;

DECLARE @dailyCounts TABLE (DayOffset int PRIMARY KEY, VisitCount int);
INSERT INTO @dailyCounts (DayOffset, VisitCount)
VALUES (6, 4), (5, 6), (4, 5), (3, 7), (2, 6), (1, 8);

DECLARE @dayOffset int = 6;
DECLARE @created int = 0;
DECLARE @updated int = 0;

BEGIN TRANSACTION;

WHILE @dayOffset >= 1
BEGIN
    DECLARE @visitCount int = (
        SELECT VisitCount FROM @dailyCounts WHERE DayOffset = @dayOffset
    );
    DECLARE @item int = 1;

    WHILE @item <= @visitCount
    BEGIN
        DECLARE @patientNumber int = 13 + ((@dayOffset + @item - 1) % 8);
        DECLARE @patientName nvarchar(256) = CONCAT(
            'Demo Patient ', RIGHT(CONCAT('0', @patientNumber), 2), ' - History'
        );
        DECLARE @doctorEmail nvarchar(256) = CONCAT(
            'demo.doctor.0', 1 + ((@item - 1) % 3), '@example.invalid'
        );
        DECLARE @patientId uniqueidentifier;
        DECLARE @doctorId uniqueidentifier;

        SELECT @patientId = Id
        FROM Patients
        WHERE ClinicId = @clinicId AND FullName = @patientName;

        SELECT @doctorId = Id
        FROM Users
        WHERE ClinicId = @clinicId AND Email = @doctorEmail;

        IF @patientId IS NULL OR @doctorId IS NULL
            THROW 51002, 'Run the API demo seed before adding dashboard history.', 1;

        DECLARE @visitId uniqueidentifier = CONVERT(
            uniqueidentifier,
            HASHBYTES(
                'MD5',
                CONCAT(CONVERT(varchar(36), @clinicId), '|dashboard|', @dayOffset, '|', @item)
            )
        );
        DECLARE @entryAtUtc datetime2 = DATEADD(
            minute,
            @item * 15,
            DATEADD(hour, 9, CONVERT(datetime2, DATEADD(day, -@dayOffset, @localDate)))
        );
        DECLARE @completedAtUtc datetime2 = DATEADD(minute, 40, @entryAtUtc);

        IF EXISTS (SELECT 1 FROM Visits WHERE Id = @visitId AND ClinicId <> @clinicId)
            THROW 51003, 'A demo visit identifier collided with another clinic.', 1;

        IF EXISTS (SELECT 1 FROM Visits WHERE Id = @visitId)
        BEGIN
            UPDATE Visits
            SET PatientId = @patientId,
                DoctorId = @doctorId,
                Status = 'Completed',
                DocumentationStatus = 'Completed',
                EntryAtUtc = @entryAtUtc,
                CompletedAtUtc = @completedAtUtc,
                ExitAtUtc = @completedAtUtc,
                UpdatedDate = SYSUTCDATETIME()
            WHERE Id = @visitId AND ClinicId = @clinicId;
            SET @updated += 1;
        END
        ELSE
        BEGIN
            INSERT INTO Visits (
                Id,
                PatientId,
                DoctorId,
                Status,
                DocumentationStatus,
                EntryAtUtc,
                CompletedAtUtc,
                ExitAtUtc,
                CreatedDate,
                ClinicId
            )
            VALUES (
                @visitId,
                @patientId,
                @doctorId,
                'Completed',
                'Completed',
                @entryAtUtc,
                @completedAtUtc,
                @completedAtUtc,
                @entryAtUtc,
                @clinicId
            );
            SET @created += 1;
        END;

        SET @item += 1;
    END;

    SET @dayOffset -= 1;
END;

COMMIT TRANSACTION;

SELECT @created AS CreatedHistoricalVisits,
       @updated AS RefreshedHistoricalVisits;
GO
