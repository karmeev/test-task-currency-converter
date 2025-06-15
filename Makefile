local_infra:
	docker network inspect currency_network >/dev/null 2>&1 || docker network create currency_network && \
	docker compose -f docker-compose.infra.yaml up -d

load_tests_in_compose:
	docker compose -f ./tests/load/docker-compose.yaml up --build -d redis
	docker compose -f ./tests/load/docker-compose.yaml up --build --exit-code-from redis-init redis-init
	docker compose -f ./tests/load/docker-compose.yaml up --build -d api
	@echo "Waiting 60 seconds for API to fully start..."
	sleep 60
	@echo "Start K6..."
	docker compose -f ./tests/load/docker-compose.yaml up --build --abort-on-container-exit k6
