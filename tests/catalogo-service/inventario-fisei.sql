-- Solo lectura. Ejecutar antes y después de actualizar el catálogo, sin escrituras concurrentes.
SET NOCOUNT ON;
USE Catalogo;
SELECT Facultad, COUNT(*) AS Carreras FROM Carreras GROUP BY Facultad ORDER BY Facultad;
SELECT COUNT(*) AS Carreras, CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS ComprobacionCarreras FROM Carreras;
SELECT COUNT(*) AS Asignaturas, CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS ComprobacionAsignaturas FROM Asignaturas;
-- CHECKSUM es una comprobación rápida de esta demostración, no un respaldo ni prueba criptográfica.
