-- ============================================================================
--  Seed: wissenschaftlich verifizierte Basis-Taxonomie (Tierreich)
--  Quelle: GBIF Backbone Taxonomy (https://www.gbif.org), abgeglichen mit ITIS.
--
--  Hinweis zu den Rängen: Dein Enum erlaubt nur Phylum/Class/Order/Family/
--  Genus/Species. Zwischenränge (Subphylum, Subfamilie, Tribus, Suborder ...)
--  werden bewusst kollabiert. Für Reptilien und Fische wird die klassische
--  Linnésche Einordnung (Klasse Reptilia mit Ordnungen Squamata/Testudines/
--  Crocodylia; Klassen Actinopterygii/Chondrichthyes mit ihren Ordnungen)
--  verwendet, weil der GBIF-Backbone dort keine durchgaengige Order-Ebene
--  fuehrt. Diese Einordnung entspricht ITIS / Catalogue of Life.
--
--  Idempotent + wiederverwendungssicher: Eltern werden ueber (name, rank)
--  aufgeloest; es wird nur eingefuegt, was noch nicht existiert. Mehrfaches
--  Ausfuehren ist gefahrlos und respektiert den Unique-Index
--  uq_taxonomy_nodes_parent_name_rank. approval_status = 1 (Approved).
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;

-- ── Phylum (rank 0) ─────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 0, 1, NULL, SYSUTCDATETIME()
FROM (VALUES
    (N'Chordata'), (N'Arthropoda'), (N'Mollusca')
) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 0 AND t.parent_guid IS NULL);

-- ── Class (rank 1) ──────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 1, 1, p.guid, SYSUTCDATETIME()
FROM (VALUES
    (N'Mammalia',       N'Chordata'),
    (N'Aves',           N'Chordata'),
    (N'Reptilia',       N'Chordata'),
    (N'Amphibia',       N'Chordata'),
    (N'Actinopterygii', N'Chordata'),
    (N'Chondrichthyes', N'Chordata'),
    (N'Insecta',        N'Arthropoda'),
    (N'Arachnida',      N'Arthropoda'),
    (N'Malacostraca',   N'Arthropoda'),
    (N'Gastropoda',     N'Mollusca'),
    (N'Cephalopoda',    N'Mollusca')
) AS v(name, parent_name)
JOIN zootify.taxonomy_nodes p ON p.name = v.parent_name AND p.rank = 0
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 1 AND t.parent_guid = p.guid);

-- ── Order (rank 2) ──────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 2, 1, p.guid, SYSUTCDATETIME()
FROM (VALUES
    (N'Carnivora',       N'Mammalia'),
    (N'Primates',        N'Mammalia'),
    (N'Proboscidea',     N'Mammalia'),
    (N'Artiodactyla',    N'Mammalia'),
    (N'Cetacea',         N'Mammalia'),
    (N'Rodentia',        N'Mammalia'),
    (N'Lagomorpha',      N'Mammalia'),
    (N'Accipitriformes', N'Aves'),
    (N'Strigiformes',    N'Aves'),
    (N'Passeriformes',   N'Aves'),
    (N'Anseriformes',    N'Aves'),
    (N'Galliformes',     N'Aves'),
    (N'Falconiformes',   N'Aves'),
    (N'Sphenisciformes', N'Aves'),
    (N'Ciconiiformes',   N'Aves'),
    (N'Columbiformes',   N'Aves'),
    (N'Squamata',        N'Reptilia'),
    (N'Testudines',      N'Reptilia'),
    (N'Crocodylia',      N'Reptilia'),
    (N'Anura',           N'Amphibia'),
    (N'Caudata',         N'Amphibia'),
    (N'Salmoniformes',   N'Actinopterygii'),
    (N'Cypriniformes',   N'Actinopterygii'),
    (N'Perciformes',     N'Actinopterygii'),
    (N'Lamniformes',     N'Chondrichthyes'),
    (N'Lepidoptera',     N'Insecta'),
    (N'Coleoptera',      N'Insecta'),
    (N'Hymenoptera',     N'Insecta'),
    (N'Odonata',         N'Insecta'),
    (N'Diptera',         N'Insecta'),
    (N'Araneae',         N'Arachnida'),
    (N'Decapoda',        N'Malacostraca'),
    (N'Stylommatophora', N'Gastropoda'),
    (N'Octopoda',        N'Cephalopoda')
) AS v(name, parent_name)
JOIN zootify.taxonomy_nodes p ON p.name = v.parent_name AND p.rank = 1
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 2 AND t.parent_guid = p.guid);

-- ── Family (rank 3) ─────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 3, 1, p.guid, SYSUTCDATETIME()
FROM (VALUES
    (N'Felidae',      N'Carnivora'),
    (N'Canidae',      N'Carnivora'),
    (N'Ursidae',      N'Carnivora'),
    (N'Hominidae',    N'Primates'),
    (N'Elephantidae', N'Proboscidea'),
    (N'Cervidae',     N'Artiodactyla'),
    (N'Suidae',       N'Artiodactyla'),
    (N'Bovidae',      N'Artiodactyla'),
    (N'Delphinidae',  N'Cetacea'),
    (N'Sciuridae',    N'Rodentia'),
    (N'Leporidae',    N'Lagomorpha'),
    (N'Accipitridae', N'Accipitriformes'),
    (N'Strigidae',    N'Strigiformes'),
    (N'Corvidae',     N'Passeriformes'),
    (N'Turdidae',     N'Passeriformes'),
    (N'Anatidae',     N'Anseriformes'),
    (N'Phasianidae',  N'Galliformes'),
    (N'Falconidae',   N'Falconiformes'),
    (N'Spheniscidae', N'Sphenisciformes'),
    (N'Ciconiidae',   N'Ciconiiformes'),
    (N'Columbidae',   N'Columbiformes'),
    (N'Lacertidae',   N'Squamata'),
    (N'Colubridae',   N'Squamata'),
    (N'Viperidae',    N'Squamata'),
    (N'Testudinidae', N'Testudines'),
    (N'Crocodylidae', N'Crocodylia'),
    (N'Ranidae',      N'Anura'),
    (N'Bufonidae',    N'Anura'),
    (N'Salamandridae',N'Caudata'),
    (N'Salmonidae',   N'Salmoniformes'),
    (N'Cyprinidae',   N'Cypriniformes'),
    (N'Percidae',     N'Perciformes'),
    (N'Lamnidae',     N'Lamniformes'),
    (N'Papilionidae', N'Lepidoptera'),
    (N'Nymphalidae',  N'Lepidoptera'),
    (N'Pieridae',     N'Lepidoptera'),
    (N'Carabidae',    N'Coleoptera'),
    (N'Coccinellidae',N'Coleoptera'),
    (N'Lucanidae',    N'Coleoptera'),
    (N'Apidae',       N'Hymenoptera'),
    (N'Vespidae',     N'Hymenoptera'),
    (N'Aeshnidae',    N'Odonata'),
    (N'Muscidae',     N'Diptera'),
    (N'Araneidae',    N'Araneae'),
    (N'Astacidae',    N'Decapoda'),
    (N'Nephropidae',  N'Decapoda'),
    (N'Helicidae',    N'Stylommatophora'),
    (N'Octopodidae',  N'Octopoda')
) AS v(name, parent_name)
JOIN zootify.taxonomy_nodes p ON p.name = v.parent_name AND p.rank = 2
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 3 AND t.parent_guid = p.guid);

-- ── Genus (rank 4) ──────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 4, 1, p.guid, SYSUTCDATETIME()
FROM (VALUES
    (N'Panthera',    N'Felidae'),
    (N'Felis',       N'Felidae'),
    (N'Lynx',        N'Felidae'),
    (N'Canis',       N'Canidae'),
    (N'Vulpes',      N'Canidae'),
    (N'Ursus',       N'Ursidae'),
    (N'Homo',        N'Hominidae'),
    (N'Loxodonta',   N'Elephantidae'),
    (N'Cervus',      N'Cervidae'),
    (N'Sus',         N'Suidae'),
    (N'Bos',         N'Bovidae'),
    (N'Tursiops',    N'Delphinidae'),
    (N'Sciurus',     N'Sciuridae'),
    (N'Oryctolagus', N'Leporidae'),
    (N'Aquila',      N'Accipitridae'),
    (N'Buteo',       N'Accipitridae'),
    (N'Bubo',        N'Strigidae'),
    (N'Corvus',      N'Corvidae'),
    (N'Turdus',      N'Turdidae'),
    (N'Anas',        N'Anatidae'),
    (N'Gallus',      N'Phasianidae'),
    (N'Falco',       N'Falconidae'),
    (N'Aptenodytes', N'Spheniscidae'),
    (N'Ciconia',     N'Ciconiidae'),
    (N'Columba',     N'Columbidae'),
    (N'Lacerta',     N'Lacertidae'),
    (N'Natrix',      N'Colubridae'),
    (N'Vipera',      N'Viperidae'),
    (N'Testudo',     N'Testudinidae'),
    (N'Crocodylus',  N'Crocodylidae'),
    (N'Rana',        N'Ranidae'),
    (N'Bufo',        N'Bufonidae'),
    (N'Salamandra',  N'Salamandridae'),
    (N'Salmo',       N'Salmonidae'),
    (N'Cyprinus',    N'Cyprinidae'),
    (N'Perca',       N'Percidae'),
    (N'Carcharodon', N'Lamnidae'),
    (N'Papilio',     N'Papilionidae'),
    (N'Aglais',      N'Nymphalidae'),
    (N'Pieris',      N'Pieridae'),
    (N'Carabus',     N'Carabidae'),
    (N'Coccinella',  N'Coccinellidae'),
    (N'Lucanus',     N'Lucanidae'),
    (N'Apis',        N'Apidae'),
    (N'Bombus',      N'Apidae'),
    (N'Vespa',       N'Vespidae'),
    (N'Aeshna',      N'Aeshnidae'),
    (N'Musca',       N'Muscidae'),
    (N'Araneus',     N'Araneidae'),
    (N'Astacus',     N'Astacidae'),
    (N'Homarus',     N'Nephropidae'),
    (N'Helix',       N'Helicidae'),
    (N'Octopus',     N'Octopodidae')
) AS v(name, parent_name)
JOIN zootify.taxonomy_nodes p ON p.name = v.parent_name AND p.rank = 3
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 4 AND t.parent_guid = p.guid);

-- ── Species (rank 5) ────────────────────────────────────────────────────────
INSERT INTO zootify.taxonomy_nodes (guid, name, rank, approval_status, parent_guid, created_at)
SELECT NEWID(), v.name, 5, 1, p.guid, SYSUTCDATETIME()
FROM (VALUES
    (N'Panthera leo',             N'Panthera'),
    (N'Panthera tigris',          N'Panthera'),
    (N'Panthera pardus',          N'Panthera'),
    (N'Felis catus',              N'Felis'),
    (N'Lynx lynx',                N'Lynx'),
    (N'Canis lupus',              N'Canis'),
    (N'Vulpes vulpes',            N'Vulpes'),
    (N'Ursus arctos',             N'Ursus'),
    (N'Ursus maritimus',          N'Ursus'),
    (N'Homo sapiens',             N'Homo'),
    (N'Loxodonta africana',       N'Loxodonta'),
    (N'Cervus elaphus',           N'Cervus'),
    (N'Sus scrofa',               N'Sus'),
    (N'Bos taurus',               N'Bos'),
    (N'Tursiops truncatus',       N'Tursiops'),
    (N'Sciurus vulgaris',         N'Sciurus'),
    (N'Oryctolagus cuniculus',    N'Oryctolagus'),
    (N'Aquila chrysaetos',        N'Aquila'),
    (N'Buteo buteo',              N'Buteo'),
    (N'Bubo bubo',                N'Bubo'),
    (N'Corvus corax',             N'Corvus'),
    (N'Turdus merula',            N'Turdus'),
    (N'Anas platyrhynchos',       N'Anas'),
    (N'Gallus gallus',            N'Gallus'),
    (N'Falco peregrinus',         N'Falco'),
    (N'Aptenodytes forsteri',     N'Aptenodytes'),
    (N'Ciconia ciconia',          N'Ciconia'),
    (N'Columba livia',            N'Columba'),
    (N'Lacerta agilis',           N'Lacerta'),
    (N'Natrix natrix',            N'Natrix'),
    (N'Vipera berus',             N'Vipera'),
    (N'Testudo hermanni',         N'Testudo'),
    (N'Crocodylus niloticus',     N'Crocodylus'),
    (N'Rana temporaria',          N'Rana'),
    (N'Bufo bufo',                N'Bufo'),
    (N'Salamandra salamandra',    N'Salamandra'),
    (N'Salmo trutta',             N'Salmo'),
    (N'Salmo salar',              N'Salmo'),
    (N'Cyprinus carpio',          N'Cyprinus'),
    (N'Perca fluviatilis',        N'Perca'),
    (N'Carcharodon carcharias',   N'Carcharodon'),
    (N'Papilio machaon',          N'Papilio'),
    (N'Aglais urticae',           N'Aglais'),
    (N'Pieris brassicae',         N'Pieris'),
    (N'Carabus coriaceus',        N'Carabus'),
    (N'Coccinella septempunctata',N'Coccinella'),
    (N'Lucanus cervus',           N'Lucanus'),
    (N'Apis mellifera',           N'Apis'),
    (N'Bombus terrestris',        N'Bombus'),
    (N'Vespa crabro',             N'Vespa'),
    (N'Aeshna cyanea',            N'Aeshna'),
    (N'Musca domestica',          N'Musca'),
    (N'Araneus diadematus',       N'Araneus'),
    (N'Astacus astacus',          N'Astacus'),
    (N'Homarus gammarus',         N'Homarus'),
    (N'Helix pomatia',            N'Helix'),
    (N'Octopus vulgaris',         N'Octopus')
) AS v(name, parent_name)
JOIN zootify.taxonomy_nodes p ON p.name = v.parent_name AND p.rank = 4
WHERE NOT EXISTS (SELECT 1 FROM zootify.taxonomy_nodes t
                  WHERE t.name = v.name AND t.rank = 5 AND t.parent_guid = p.guid);

COMMIT;
