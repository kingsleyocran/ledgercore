.PHONY: build test test-v clean restore format check

build:
	dotnet build LedgerCore.slnx

test:
	dotnet test LedgerCore.slnx --no-build -v minimal

test-v:
	dotnet test LedgerCore.slnx --no-build -v normal

test-build:
	dotnet test LedgerCore.slnx -v minimal

restore:
	dotnet restore LedgerCore.slnx

clean:
	dotnet clean LedgerCore.slnx
	find . -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true

format:
	dotnet format LedgerCore.slnx

check:
	dotnet format LedgerCore.slnx --verify-no-changes
