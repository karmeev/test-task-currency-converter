local_infra:
	docker network inspect currency_network >/dev/null 2>&1 || docker network create currency_network && \
	docker compose -f docker-compose.infra.yaml up -d

load_tests_in_compose:
	@pwd
	docker-compose -f ../../tests/load/docker-compose.yml up --build --abort-on-container-exit
