local_infra:
	docker network inspect currency_network >/dev/null 2>&1 || docker network create currency_network && \
	docker compose -f docker-compose.infra.yaml up -d

load_tests_in_compose:
	docker compose -f ./tests/load/docker-compose.yaml up --build -d redis
	docker compose -f ./tests/load/docker-compose.yaml up --build --exit-code-from redis-init redis-init
	docker compose -f ./tests/load/docker-compose.yaml up --build api
	@echo "Waiting 120 seconds for API to fully start..."
	sleep 120
	@echo "Start K6..."
	docker compose -f ./tests/load/docker-compose.yaml up --build --abort-on-container-exit k6

unit_tests:
	dotnet test ./tests/unit-tests/Currency.Api.Tests/Currency.Api.Tests.csproj \
				--configuration Release \
                --no-restore \
                --logger "console;verbosity=detailed"
                
	dotnet test ./tests/unit-tests/Currency.Data.Tests/Currency.Data.Tests.csproj \
        		--configuration Release \
        		--no-restore \
        		--logger "console;verbosity=detailed"
        		
	dotnet test ./tests/unit-tests/Currency.Facades.Tests/Currency.Facades.Tests.csproj \
        		--configuration Release \
        		--no-restore \
        		--logger "console;verbosity=detailed"
        		
	dotnet test ./tests/unit-tests/Currency.Infrastructure.Tests/Currency.Infrastructure.Tests.csproj \
        		--configuration Release \
        		--no-restore \
        		--logger "console;verbosity=detailed"
        		
	dotnet test ./tests/unit-tests/Currency.Services.Tests/Currency.Services.Tests.csproj \
        		--configuration Release \
        		--no-restore \
        		--logger "console;verbosity=detailed"

integration_tests:
	dotnet test ./tests/integrations-tests/Currency.IntegrationTests.Infrastructure/Currency.IntegrationTests.Infrastructure.csproj \
				--configuration Release \
                --no-restore \
                --logger "console;verbosity=detailed"
		
wiremock_up:
	docker-compose -f ./tests/integrations-tests/Currency.IntegrationTests.Infrastructure/docker-compose.yaml up -d
	@echo "Waiting for WireMock to be ready..."
	@for i in $$(seq 1 30); do \
		curl -s http://localhost:8080/__admin && echo "WireMock ready!" && exit 0 || \
		(echo "Waiting for WireMock..." && sleep 2); \
	done; \
	echo "WireMock failed to start in time" && exit 1

wiremock_down:
	docker-compose -f ./tests/integrations-tests/Currency.IntegrationTests.Infrastructure/docker-compose.yaml down