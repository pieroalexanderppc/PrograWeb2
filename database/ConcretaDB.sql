/* =====================================================================
   Concreta - Sistema web de apoyo al diseño de mezclas de concreto
   Reto 2 Hackathon Innova Sur 2026 (Yura S.A.)
   Script de base de datos - SQL Server 2019 / 2022 (local)
   ---------------------------------------------------------------------
   Ejecutar en SSMS o con:
     sqlcmd -S localhost -E -f 65001 -i database\ConcretaDB.sql
   (-f 65001 lee el archivo como UTF-8 para conservar las tildes)
   Para reinstalar desde cero:
     sqlcmd -S localhost -E -Q "DROP DATABASE ConcretaDB"
   ===================================================================== */

IF DB_ID('ConcretaDB') IS NULL
    CREATE DATABASE ConcretaDB;
GO
USE ConcretaDB;
GO

/* =========================== SEGURIDAD =============================== */

CREATE TABLE Rol (
    IdRol           INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(30)  NOT NULL UNIQUE,
    Descripcion     VARCHAR(150) NULL
);

CREATE TABLE Usuario (
    IdUsuario       INT IDENTITY(1,1) PRIMARY KEY,
    IdRol           INT           NOT NULL,
    Nombres         VARCHAR(60)   NOT NULL,
    Apellidos       VARCHAR(60)   NOT NULL,
    Correo          VARCHAR(100)  NOT NULL UNIQUE,
    ContrasenaHash  VARCHAR(255)  NOT NULL,
    Activo          BIT           NOT NULL DEFAULT 1,
    FechaRegistro   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES Rol(IdRol)
);

/* =========================== MATERIALES ============================== */

CREATE TABLE TipoMaterial (
    IdTipoMaterial  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(40) NOT NULL UNIQUE   -- Cemento, Agregado fino, Agregado grueso, Agua, Aditivo
);

CREATE TABLE Material (
    IdMaterial          INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoMaterial      INT            NOT NULL,
    Nombre              VARCHAR(100)   NOT NULL,
    Proveedor           VARCHAR(100)   NULL,
    PesoEspecifico      DECIMAL(5,3)   NOT NULL CHECK (PesoEspecifico > 0),          -- g/cm3
    PorcentajeAdicion   DECIMAL(5,2)   NULL CHECK (PorcentajeAdicion BETWEEN 0 AND 100), -- solo cementos
    ModuloFineza        DECIMAL(4,2)   NULL,                                         -- agregado fino
    TamanoMaxNominal    VARCHAR(10)    NULL,                                         -- agregado grueso (ej. 3/4")
    PesoUnitarioCompactado DECIMAL(7,2) NULL,                                        -- kg/m3, agregado grueso
    Absorcion           DECIMAL(5,2)   NULL,                                         -- %
    ContenidoHumedad    DECIMAL(5,2)   NULL,                                         -- %
    CostoKg             DECIMAL(10,4)  NOT NULL CHECK (CostoKg >= 0),                -- S/ por kg
    FactorCO2Kg         DECIMAL(10,5)  NOT NULL CHECK (FactorCO2Kg >= 0),            -- kg CO2e por kg
    Activo              BIT            NOT NULL DEFAULT 1,
    CONSTRAINT FK_Material_Tipo FOREIGN KEY (IdTipoMaterial) REFERENCES TipoMaterial(IdTipoMaterial)
);

/* ================ TABLAS DE PARAMETROS ACI 211.1 ===================== */

CREATE TABLE ParametroAguaAire (          -- agua de mezcla (L/m3) y aire atrapado (%)
    IdParametro      INT IDENTITY(1,1) PRIMARY KEY,
    TamanoMaxNominal VARCHAR(10)  NOT NULL,
    AsentamientoMin  DECIMAL(4,1) NOT NULL,   -- pulgadas
    AsentamientoMax  DECIMAL(4,1) NOT NULL,
    AguaLitrosM3     DECIMAL(6,1) NOT NULL,
    AireAtrapado     DECIMAL(4,2) NOT NULL,
    CONSTRAINT UQ_AguaAire UNIQUE (TamanoMaxNominal, AsentamientoMin)
);

CREATE TABLE ParametroRelacionAC (        -- relación agua/cemento según f'cr
    IdParametro      INT IDENTITY(1,1) PRIMARY KEY,
    FcrKgCm2         INT          NOT NULL UNIQUE,
    RelacionAC       DECIMAL(4,3) NOT NULL
);

CREATE TABLE ParametroAgregadoGrueso (    -- volumen de agregado grueso compactado por m3
    IdParametro      INT IDENTITY(1,1) PRIMARY KEY,
    TamanoMaxNominal VARCHAR(10)  NOT NULL,
    ModuloFineza     DECIMAL(4,2) NOT NULL,
    VolumenM3        DECIMAL(4,3) NOT NULL,
    CONSTRAINT UQ_AgregadoGrueso UNIQUE (TamanoMaxNominal, ModuloFineza)
);

/* =========================== DISEÑO ================================== */

CREATE TABLE Proyecto (
    IdProyecto      INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario       INT           NOT NULL,
    Nombre          VARCHAR(120)  NOT NULL,
    Cliente         VARCHAR(120)  NULL,
    Ubicacion       VARCHAR(150)  NULL,
    Descripcion     VARCHAR(300)  NULL,
    FechaCreacion   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Proyecto_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario)
);

CREATE TABLE DisenoMezcla (
    IdDiseno            INT IDENTITY(1,1) PRIMARY KEY,
    IdProyecto          INT            NOT NULL,
    IdUsuario           INT            NOT NULL,
    Codigo              VARCHAR(20)    NOT NULL UNIQUE,            -- ej. DM-2026-0001
    Descripcion         VARCHAR(200)   NULL,
    FcKgCm2             INT            NOT NULL CHECK (FcKgCm2 BETWEEN 100 AND 700),
    FcrKgCm2            INT            NULL,                       -- resistencia promedio requerida
    AsentamientoPulg    DECIMAL(4,1)   NOT NULL,
    TamanoMaxNominal    VARCHAR(10)    NOT NULL,
    RelacionAC          DECIMAL(4,3)   NULL,
    AireAtrapado        DECIMAL(4,2)   NULL,
    MetodoDiseno        VARCHAR(30)    NOT NULL DEFAULT 'ACI 211.1',
    CostoTotalM3        DECIMAL(10,2)  NULL,                       -- S/ por m3
    HuellaCO2M3         DECIMAL(10,2)  NULL,                       -- kg CO2e por m3
    Estado              VARCHAR(15)    NOT NULL DEFAULT 'Borrador'
                        CHECK (Estado IN ('Borrador','Calculado','Ensayado','Aprobado','Descartado')),
    FechaCreacion       DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Diseno_Proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyecto(IdProyecto),
    CONSTRAINT FK_Diseno_Usuario  FOREIGN KEY (IdUsuario)  REFERENCES Usuario(IdUsuario)
);

CREATE TABLE DetalleMezcla (
    IdDetalle           INT IDENTITY(1,1) PRIMARY KEY,
    IdDiseno            INT            NOT NULL,
    IdMaterial          INT            NOT NULL,
    CantidadKgM3        DECIMAL(10,2)  NOT NULL CHECK (CantidadKgM3 >= 0),
    VolumenAbsolutoM3   DECIMAL(8,4)   NULL,
    CostoParcial        DECIMAL(10,2)  NULL,
    CO2Parcial          DECIMAL(10,3)  NULL,
    CONSTRAINT FK_Detalle_Diseno   FOREIGN KEY (IdDiseno)   REFERENCES DisenoMezcla(IdDiseno) ON DELETE CASCADE,
    CONSTRAINT FK_Detalle_Material FOREIGN KEY (IdMaterial) REFERENCES Material(IdMaterial),
    CONSTRAINT UQ_Detalle UNIQUE (IdDiseno, IdMaterial)
);

CREATE TABLE EnsayoLaboratorio (
    IdEnsayo            INT IDENTITY(1,1) PRIMARY KEY,
    IdDiseno            INT            NOT NULL,
    IdUsuario           INT            NOT NULL,
    TipoEnsayo          VARCHAR(30)    NOT NULL
                        CHECK (TipoEnsayo IN ('Compresion','Asentamiento','Temperatura','PesoUnitario')),
    EdadDias            INT            NULL CHECK (EdadDias IN (3,7,14,28,56)),
    ValorObtenido       DECIMAL(10,2)  NOT NULL,
    Unidad              VARCHAR(15)    NOT NULL,                   -- kg/cm2, pulg, °C, kg/m3
    FechaEnsayo         DATE           NOT NULL,
    Observaciones       VARCHAR(250)   NULL,
    CONSTRAINT FK_Ensayo_Diseno  FOREIGN KEY (IdDiseno)  REFERENCES DisenoMezcla(IdDiseno),
    CONSTRAINT FK_Ensayo_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario)
);

/* ===================== COMPARACIÓN Y RECOMENDACIÓN =================== */

CREATE TABLE Comparacion (
    IdComparacion       INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario           INT            NOT NULL,
    Nombre              VARCHAR(120)   NOT NULL,
    Criterio            VARCHAR(15)    NOT NULL
                        CHECK (Criterio IN ('Costo','CO2','Balanceado')),
    IdDisenoRecomendado INT            NULL,
    Justificacion       VARCHAR(500)   NULL,
    FechaComparacion    DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Comparacion_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Comparacion_Diseno  FOREIGN KEY (IdDisenoRecomendado) REFERENCES DisenoMezcla(IdDiseno)
);

CREATE TABLE ComparacionDetalle (
    IdComparacion       INT            NOT NULL,
    IdDiseno            INT            NOT NULL,
    Puntaje             DECIMAL(6,2)   NULL,                       -- 0 a 100
    Posicion            INT            NULL,
    CumpleResistencia   BIT            NOT NULL DEFAULT 0,
    CONSTRAINT PK_ComparacionDetalle PRIMARY KEY (IdComparacion, IdDiseno),
    CONSTRAINT FK_CompDet_Comparacion FOREIGN KEY (IdComparacion) REFERENCES Comparacion(IdComparacion) ON DELETE CASCADE,
    CONSTRAINT FK_CompDet_Diseno      FOREIGN KEY (IdDiseno)      REFERENCES DisenoMezcla(IdDiseno)
);
GO

/* =========================== ÍNDICES ================================= */
CREATE INDEX IX_Diseno_Proyecto ON DisenoMezcla(IdProyecto);
CREATE INDEX IX_Detalle_Diseno  ON DetalleMezcla(IdDiseno);
CREATE INDEX IX_Ensayo_Diseno   ON EnsayoLaboratorio(IdDiseno, EdadDias);
CREATE INDEX IX_Material_Tipo   ON Material(IdTipoMaterial);
GO

/* ======================= PROCEDIMIENTO Y VISTA ======================= */

-- Recalcula costo y huella de carbono de un diseño a partir de su detalle
CREATE OR ALTER PROCEDURE sp_CalcularTotalesDiseno
    @IdDiseno INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE d
       SET d.CostoParcial = ROUND(d.CantidadKgM3 * m.CostoKg, 2),
           d.CO2Parcial   = ROUND(d.CantidadKgM3 * m.FactorCO2Kg, 3)
      FROM DetalleMezcla d
      JOIN Material m ON m.IdMaterial = d.IdMaterial
     WHERE d.IdDiseno = @IdDiseno;

    UPDATE DisenoMezcla
       SET CostoTotalM3 = (SELECT SUM(CostoParcial) FROM DetalleMezcla WHERE IdDiseno = @IdDiseno),
           HuellaCO2M3  = (SELECT SUM(CO2Parcial)   FROM DetalleMezcla WHERE IdDiseno = @IdDiseno),
           Estado       = CASE WHEN Estado = 'Borrador' THEN 'Calculado' ELSE Estado END
     WHERE IdDiseno = @IdDiseno;
END;
GO

-- Resumen de cada diseño con su resistencia real a 28 días: promedio de las probetas (si existen)
CREATE OR ALTER VIEW vw_ResumenDiseno AS
SELECT  dm.IdDiseno,
        dm.Codigo,
        p.Nombre            AS Proyecto,
        dm.FcKgCm2,
        dm.RelacionAC,
        cem.Nombre          AS Cemento,
        cem.PorcentajeAdicion,
        dc.CantidadKgM3     AS CementoKgM3,
        dm.CostoTotalM3,
        dm.HuellaCO2M3,
        e28.ValorObtenido   AS Resistencia28d,
        CASE WHEN e28.ValorObtenido >= dm.FcKgCm2 THEN 'Cumple'
             WHEN e28.ValorObtenido IS NULL      THEN 'Sin ensayo'
             ELSE 'No cumple' END AS Resultado,
        dm.Estado
FROM    DisenoMezcla dm
JOIN    Proyecto p          ON p.IdProyecto = dm.IdProyecto
LEFT JOIN DetalleMezcla dc  ON dc.IdDiseno = dm.IdDiseno
                           AND dc.IdMaterial IN (SELECT m.IdMaterial FROM Material m
                                                  JOIN TipoMaterial t ON t.IdTipoMaterial = m.IdTipoMaterial
                                                 WHERE t.Nombre = 'Cemento')
LEFT JOIN Material cem      ON cem.IdMaterial = dc.IdMaterial
OUTER APPLY (SELECT AVG(e.ValorObtenido) AS ValorObtenido
               FROM EnsayoLaboratorio e
              WHERE e.IdDiseno = dm.IdDiseno AND e.TipoEnsayo = 'Compresion' AND e.EdadDias = 28) e28;
GO

/* ========================= DATOS INICIALES =========================== */
-- Nota: costos y factores de CO2 son valores REFERENCIALES para el prototipo.

INSERT INTO Rol (Nombre, Descripcion) VALUES
('Administrador', 'Gestiona usuarios, materiales y parámetros'),
('Ingeniero',     'Crea y compara diseños de mezcla'),
('Laboratorista', 'Registra ensayos de laboratorio');

-- La aplicación (DbSeeder) reemplaza HASH_PENDIENTE por el hash de PasswordHasher<Usuario>
INSERT INTO Usuario (IdRol, Nombres, Apellidos, Correo, ContrasenaHash) VALUES
(1, 'Admin',  'Concreta', 'admin@concreta.pe',   'HASH_PENDIENTE'),
(2, 'Carlos', 'Mamani',   'cmamani@concreta.pe', 'HASH_PENDIENTE'),
(3, 'Lucía',  'Quispe',   'lquispe@concreta.pe', 'HASH_PENDIENTE');

INSERT INTO TipoMaterial (Nombre) VALUES
('Cemento'), ('Agregado fino'), ('Agregado grueso'), ('Agua'), ('Aditivo');

INSERT INTO Material (IdTipoMaterial, Nombre, Proveedor, PesoEspecifico, PorcentajeAdicion, ModuloFineza, TamanoMaxNominal, PesoUnitarioCompactado, Absorcion, ContenidoHumedad, CostoKg, FactorCO2Kg) VALUES
(1, 'Cemento Portland Tipo I',                 'Referencial', 3.150,  0.00, NULL, NULL,   NULL, NULL, NULL, 0.6800, 0.90000),
(1, 'Cemento Portland Puzolánico Tipo IP',     'Referencial', 2.850, 30.00, NULL, NULL,   NULL, NULL, NULL, 0.6400, 0.63000),
(1, 'Cemento de alta adición (escoria/puzolana)','Referencial', 2.800, 45.00, NULL, NULL, NULL, NULL, NULL, 0.6200, 0.50000),
(2, 'Arena gruesa de río',                     'Cantera local', 2.650, NULL, 2.80, NULL,   NULL, 1.20, 3.00, 0.0450, 0.00500),
(3, 'Piedra chancada 3/4"',                    'Cantera local', 2.700, NULL, NULL, '3/4"', 1600, 0.80, 1.00, 0.0550, 0.00750),
(4, 'Agua potable',                            'Red pública',   1.000, NULL, NULL, NULL,   NULL, NULL, NULL, 0.0050, 0.00030),
(5, 'Aditivo plastificante',                   'Referencial',   1.200, NULL, NULL, NULL,   NULL, NULL, NULL, 9.5000, 1.50000);

-- ACI 211.1 - Agua de mezcla (concreto sin aire incorporado) y aire atrapado
INSERT INTO ParametroAguaAire (TamanoMaxNominal, AsentamientoMin, AsentamientoMax, AguaLitrosM3, AireAtrapado) VALUES
('3/8"',1,2,207,3.0), ('1/2"',1,2,199,2.5), ('3/4"',1,2,190,2.0), ('1"',1,2,179,1.5), ('1 1/2"',1,2,166,1.0), ('2"',1,2,154,0.5),
('3/8"',3,4,228,3.0), ('1/2"',3,4,216,2.5), ('3/4"',3,4,205,2.0), ('1"',3,4,193,1.5), ('1 1/2"',3,4,181,1.0), ('2"',3,4,169,0.5),
('3/8"',6,7,243,3.0), ('1/2"',6,7,228,2.5), ('3/4"',6,7,216,2.0), ('1"',6,7,202,1.5), ('1 1/2"',6,7,190,1.0), ('2"',6,7,178,0.5);

-- ACI 211.1 - Relación agua/cemento (sin aire incorporado)
INSERT INTO ParametroRelacionAC (FcrKgCm2, RelacionAC) VALUES
(150,0.800), (200,0.700), (250,0.620), (300,0.550), (350,0.480), (400,0.430), (450,0.380);

-- ACI 211.1 - Volumen de agregado grueso por m3 de concreto
INSERT INTO ParametroAgregadoGrueso (TamanoMaxNominal, ModuloFineza, VolumenM3) VALUES
('3/8"',2.40,0.50),('3/8"',2.60,0.48),('3/8"',2.80,0.46),('3/8"',3.00,0.44),
('1/2"',2.40,0.59),('1/2"',2.60,0.57),('1/2"',2.80,0.55),('1/2"',3.00,0.53),
('3/4"',2.40,0.66),('3/4"',2.60,0.64),('3/4"',2.80,0.62),('3/4"',3.00,0.60),
('1"',2.40,0.71),('1"',2.60,0.69),('1"',2.80,0.67),('1"',3.00,0.65),
('1 1/2"',2.40,0.76),('1 1/2"',2.60,0.74),('1 1/2"',2.80,0.72),('1 1/2"',3.00,0.70),
('2"',2.40,0.78),('2"',2.60,0.76),('2"',2.80,0.74),('2"',3.00,0.72);

-- Proyecto y dos diseños de ejemplo (mismo f'c, distinto cemento) para la comparación
INSERT INTO Proyecto (IdUsuario, Nombre, Cliente, Ubicacion, Descripcion) VALUES
(2, 'Losa aligerada vivienda multifamiliar', 'Cliente demo', 'Tacna', 'Concreto f''c = 210 kg/cm2 para losas y vigas');

INSERT INTO DisenoMezcla (IdProyecto, IdUsuario, Codigo, Descripcion, FcKgCm2, FcrKgCm2, AsentamientoPulg, TamanoMaxNominal, RelacionAC, AireAtrapado) VALUES
(1, 2, 'DM-2026-0001', 'f''c 210 con cemento Tipo I',  210, 294, 4.0, '3/4"', 0.558, 2.0),
(1, 2, 'DM-2026-0002', 'f''c 210 con cemento Tipo IP', 210, 294, 4.0, '3/4"', 0.558, 2.0);

INSERT INTO DetalleMezcla (IdDiseno, IdMaterial, CantidadKgM3) VALUES
(1, 1, 367), (1, 4, 771), (1, 5, 992), (1, 6, 205),
(2, 2, 367), (2, 4, 739), (2, 5, 992), (2, 6, 205);

EXEC sp_CalcularTotalesDiseno 1;
EXEC sp_CalcularTotalesDiseno 2;

INSERT INTO EnsayoLaboratorio (IdDiseno, IdUsuario, TipoEnsayo, EdadDias, ValorObtenido, Unidad, FechaEnsayo) VALUES
(1, 3, 'Compresion', 7,  168, 'kg/cm2', '2026-10-01'),
(1, 3, 'Compresion', 28, 248, 'kg/cm2', '2026-10-22'),
(2, 3, 'Compresion', 7,  142, 'kg/cm2', '2026-10-01'),
(2, 3, 'Compresion', 28, 236, 'kg/cm2', '2026-10-22');

INSERT INTO Comparacion (IdUsuario, Nombre, Criterio, IdDisenoRecomendado, Justificacion) VALUES
(2, 'f''c 210: Tipo I vs Tipo IP', 'Balanceado', 2,
 'Se recomienda DM-2026-0002: cumple f''c = 210 kg/cm² a 28 días (236 kg/cm²) con 29.0 % menos CO2 y 4.7 % menos costo que DM-2026-0001.');

-- Puntajes reales de RecomendacionService con criterio Balanceado
INSERT INTO ComparacionDetalle (IdComparacion, IdDiseno, Puntaje, Posicion, CumpleResistencia) VALUES
(1, 1, 0.00, 2, 1),
(1, 2, 100.00, 1, 1);

-- Ensayados: los dos diseños tienen resultado a 28 días
UPDATE DisenoMezcla SET Estado = 'Ensayado' WHERE IdDiseno IN (1, 2);
GO

-- Verificación: Tipo I ≈ S/ 339.85 y 341.66 kg CO2e; Tipo IP ≈ S/ 323.73 y 242.4 kg CO2e
SELECT * FROM vw_ResumenDiseno;
