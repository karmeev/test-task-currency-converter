UNIT_TESTS=./tests/unit-tests
INTEGRATION_TESTS=./tests/integrations-tests

UNIT_TEST_PROJECTS := \
    Currency.Api.Tests/Currency.Api.Tests.csproj \
    Currency.Data.Tests/Currency.Data.Tests.csproj \
    Currency.Facades.Tests/Currency.Facades.Tests.csproj \
    Currency.Infrastructure.Tests/Currency.Infrastructure.Tests.csproj \
    Currency.Services.Tests/Currency.Services.Tests.csproj \

INTEGRATION_TEST_PROJECTS := \
    Currency.IntegrationTests.Infrastructure/Currency.IntegrationTests.Infrastructure.csproj \

local_infra:
	docker network inspect currency_network >/dev/null 2>&1 || docker network create currency_network && \
	docker compose -f docker-compose.infra.yaml up -d

test:
	@if [ "$(CATEGORY)" = "Unit" ]; then \
		TEST_PATH=$(UNIT_TESTS); \
		PROJECTS="$(UNIT_TEST_PROJECTS)"; \
	elif [ "$(CATEGORY)" = "Integration" ]; then \
		TEST_PATH=$(INTEGRATION_TESTS); \
		PROJECTS="$(INTEGRATION_TEST_PROJECTS)"; \
	else \
		echo "Unsupported CATEGORY '$(CATEGORY)'. Use 'Unit' or 'Integration'."; \
		exit 1; \
	fi; \
	for proj in $$PROJECTS; do \
		echo "Running $(CATEGORY) tests in $$proj..."; \
		dotnet test $$TEST_PATH/$$proj \
			--configuration Release \
			--no-restore \
			--logger "console;verbosity=detailed" \
			--filter "Category=$(CATEGORY)"; \
	done

coverage:
	@for proj in $(UNIT_TEST_PROJECTS); do \
		echo "Running coverage for $$proj..."; \
		dotnet test $(UNIT_TESTS)/$$proj \
			--configuration Release \
			--collect:"XPlat Code Coverage" \
			--results-directory ./TestResults \
			--logger "trx;LogFileName=test-results.trx"; \
	done

	reportgenerator \
		-reports:./TestResults/**/coverage.cobertura.xml \
		-targetdir:./TestResults/CoverageReport \
		-reporttypes:MarkdownSummaryGithub

integration_tests:
	dotnet test ${INTEGRATION_TESTS}/Currency.IntegrationTests.Infrastructure/Currency.IntegrationTests.Infrastructure.csproj \
				--configuration Release \
                --no-restore \
                --logger "console;verbosity=detailed"
		
wiremock_up:
	docker compose -f ${INTEGRATION_TESTS}/Currency.IntegrationTests.Infrastructure/docker-compose.yaml up -d
	@echo "Waiting for WireMock to be ready..."
	sleep 1s

wiremock_down:
	docker compose -f ${INTEGRATION_TESTS}/Currency.IntegrationTests.Infrastructure/docker-compose.yaml down