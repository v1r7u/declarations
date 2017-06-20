BULK INSERT dbo.[NamePart]
    FROM 'D:\projects\declarations\Declarations\Declarations.Runner\bin\Debug\nameParts.csv'
    WITH
    (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',  --CSV field delimiter
    ROWTERMINATOR = '\n',   --Use to shift the control to next row
    TABLOCK,
    CODEPAGE = 65001 
    )

GO

BULK INSERT dbo.[Name]
    FROM 'D:\projects\declarations\Declarations\Declarations.Runner\bin\Debug\names.csv'
    WITH
    (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK,
    CODEPAGE = 65001 
    )

GO

BULK INSERT dbo.[Person]
    FROM 'D:\projects\declarations\Declarations\Declarations.Runner\bin\Debug\persons.csv'
    WITH
    (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK,
    CODEPAGE = 65001 
    )