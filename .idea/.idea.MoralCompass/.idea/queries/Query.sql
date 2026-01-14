-- Проверяем результат для шестой заповеди
SELECT
    'Всего дилем для "Держи слово"' as check_name,
    COUNT(*) as count
FROM "DiscussionItems" di
         JOIN "Topics" t ON di."TopicId" = t."Id"
WHERE t."CoreThesisId" = '00000006-0000-0000-0000-000000000006'
UNION ALL
SELECT
    'По темам: ' || t."Title",
    COUNT(di."Id")
FROM "Topics" t
         LEFT JOIN "DiscussionItems" di ON t."Id" = di."TopicId"
WHERE t."CoreThesisId" = '00000006-0000-0000-0000-000000000006'
GROUP BY t."Id", t."Title"
ORDER BY check_name;