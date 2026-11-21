.PHONY: help init up down restart logs ps build preset-sandbox preset-aureumro validate test-web smoke reset-db

help:
	@echo "AureumRO — Servidor de Testes (rAthena Pré-Renewal 13.3 + Painel Web)"
	@echo ""
	@echo "Comandos disponíveis:"
	@echo "  make init             Cria .env a partir de .env.example (se não existir)"
	@echo "  make up               Compila (se necessário) e sobe toda a stack em segundo plano"
	@echo "  make down             Para e remove os containers (preserva banco de dados)"
	@echo "  make restart          Reinicia o rAthena e o Painel Web"
	@echo "  make logs             Exibe logs em tempo real do rAthena"
	@echo "  make ps               Exibe status dos containers"
	@echo "  make preset-sandbox   Alterna rates para Sandbox (100x/100x/50x)"
	@echo "  make preset-aureumro  Alterna rates para AureumRO Oficial (5x/3x/1x)"
	@echo "  make validate         Valida sintaxe dos NPCs e configurações locais"
	@echo "  make test-web         Executa testes automatizados do Painel Web"
	@echo "  make smoke            Executa verificação de saúde da stack em execução"
	@echo "  make reset-db         Apaga o volume do banco de dados e recria do zero (CUIDADO)"

init:
	@if [ ! -f .env ]; then cp .env.example .env && echo "Arquivo .env criado a partir de .env.example"; else echo ".env já existe"; fi

up: init
	docker compose up -d --build

build: init
	docker compose build

down:
	docker compose down

restart:
	docker compose restart rathena webpanel

logs:
	docker compose logs -f rathena

ps:
	docker compose ps

preset-sandbox:
	./scripts/switch-preset.sh sandbox

preset-aureumro:
	./scripts/switch-preset.sh aureumro

validate:
	python3 ./scripts/validate-scripts.py

test-web:
	cd webpanel && npm test

smoke:
	./scripts/smoke-test.sh

reset-db:
	docker compose down -v
	docker compose up -d
