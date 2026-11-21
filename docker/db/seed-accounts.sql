-- =============================================================================
-- AureumRO Servidor de Testes — Contas Iniciais (idempotente)
-- =============================================================================

-- 1) Conta de comunicação interna entre login/char/map-server (account_id = 1)
INSERT INTO `login` (`account_id`, `userid`, `user_pass`, `sex`, `email`, `group_id`)
VALUES (1, '${INTER_SERVER_USER}', '${INTER_SERVER_PASS}', 'S', 'inter@aureumro.local', 0)
ON DUPLICATE KEY UPDATE
  `userid` = VALUES(`userid`),
  `user_pass` = VALUES(`user_pass`),
  `sex` = 'S';

-- 2) Conta Administrador / GM (group_id = 99)
INSERT INTO `login` (`account_id`, `userid`, `user_pass`, `sex`, `email`, `group_id`)
VALUES (2000000, '${GM_ADMIN_USER}', '${GM_ADMIN_PASS}', 'M', 'admin@aureumro.local', 99)
ON DUPLICATE KEY UPDATE
  `group_id` = 99;

-- 3) Conta Tester Padrão (group_id = 1 - Super Player com comandos de teste)
INSERT INTO `login` (`account_id`, `userid`, `user_pass`, `sex`, `email`, `group_id`)
VALUES (2000001, '${TESTER_USER}', '${TESTER_PASS}', 'M', 'tester@aureumro.local', 1)
ON DUPLICATE KEY UPDATE
  `group_id` = 1;
