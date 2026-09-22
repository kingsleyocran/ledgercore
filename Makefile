.PHONY: build test test-v clean restore format check pack

DOTNET := DOTNET_ROOT=$(HOME)/.dotnet $(HOME)/.dotnet/dotnet

build:
	$(DOTNET) build LedgerCore.slnx

test:
	$(DOTNET) test LedgerCore.slnx --no-build -v minimal

test-v:
	$(DOTNET) test LedgerCore.slnx --no-build -v normal

test-build:
	$(DOTNET) test LedgerCore.slnx -v minimal

restore:
	$(DOTNET) restore LedgerCore.slnx

clean:
	$(DOTNET) clean LedgerCore.slnx
	find . -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true

format:
	$(DOTNET) format LedgerCore.slnx

check:
	$(DOTNET) format LedgerCore.slnx --verify-no-changes

pack:
	rm -rf nupkgs
	$(DOTNET) pack src/LedgerCore.Application/LedgerCore.Application.csproj -c Release -o ./nupkgs
	$(DOTNET) pack src/LedgerCore.Infrastructure/LedgerCore.Infrastructure.csproj -c Release -o ./nupkgs
