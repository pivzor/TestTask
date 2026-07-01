SELECT
    c.id,
    c.full_name,

    d.id AS deal_id,
    d.title,
    d.description,

    s.stage_name,
    s.priority,
    s.status,
    s.is_current,
    s.date_start,
    s.date_end

FROM counterparties c

JOIN deals d
    ON d.counterparty_id = c.id

JOIN deal_stages s
    ON s.deal_id = d.id

WHERE c.id = 1

ORDER BY
    d.id,
    s.is_current DESC,
    s.priority;