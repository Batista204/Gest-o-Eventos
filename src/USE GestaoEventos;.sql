USE GestaoEventos;

-- 1. Migration aplicada
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;

-- 2. Tipos das colunas das nossas tabelas
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Eventos','Inscricoes','Presencas','Certificados')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- 3. Índices únicos (fora as chaves primárias)
SELECT t.name AS Tabela, i.name AS Indice
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.is_unique = 1 AND i.is_primary_key = 0
  AND t.name IN ('AspNetUsers','Inscricoes','Presencas','Certificados')
ORDER BY t.name;

-- 4. Restrições de verificação (CHECK)
SELECT name, definition FROM sys.check_constraints;