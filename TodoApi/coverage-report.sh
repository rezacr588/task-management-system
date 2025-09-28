#!/bin/bash

# Automated Test Coverage Report Generation
# This script runs all tests and generates comprehensive coverage reports

set -e  # Exit on any error

echo "🧪 Starting Automated Coverage Report Generation..."

# Clean previous results
echo "🧹 Cleaning previous test results..."
rm -rf TestResults/
rm -rf ../coveragereport/
find . -name "coverage.cobertura.xml" -delete

# Build the solution
echo "🔨 Building solution..."
dotnet build --configuration Release

# Run all tests with coverage
echo "🔬 Running tests with coverage collection..."

# Unit Tests
echo "  → Running Unit Tests..."
dotnet test TodoApi.Tests.Unit/TodoApi.Tests.Unit.csproj \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults/Unit/ \
    --settings:../coverage.runsettings \
    --logger "trx;LogFileName=unit-tests.trx" \
    --logger "console;verbosity=normal"

# Integration Tests  
echo "  → Running Integration Tests..."
dotnet test TodoApi.Tests.Integration/TodoApi.Tests.Integration.csproj \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults/Integration/ \
    --settings:../coverage.runsettings \
    --logger "trx;LogFileName=integration-tests.trx" \
    --logger "console;verbosity=normal"

# E2E Tests
echo "  → Running E2E Tests..."
dotnet test TodoApi.Tests.E2E/TodoApi.Tests.E2E.csproj \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults/E2E/ \
    --settings:../coverage.runsettings \
    --logger "trx;LogFileName=e2e-tests.trx" \
    --logger "console;verbosity=normal"

# BDD Tests
echo "  → Running BDD Tests..."
dotnet test TodoApi.Tests.BDD/TodoApi.Tests.BDD.csproj \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults/BDD/ \
    --settings:../coverage.runsettings \
    --logger "trx;LogFileName=bdd-tests.trx" \
    --logger "console;verbosity=normal" || echo "⚠️  BDD tests failed - continuing with coverage report"

# Performance Tests (optional - may fail in CI)
echo "  → Running Performance Tests..."
dotnet test TodoApi.Tests.Performance/TodoApi.Tests.Performance.csproj \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults/Performance/ \
    --settings:../coverage.runsettings \
    --logger "trx;LogFileName=performance-tests.trx" \
    --logger "console;verbosity=normal" || echo "⚠️  Performance tests failed - continuing with coverage report"

# Generate combined coverage report
echo "📊 Generating combined coverage report..."

# Install ReportGenerator if not present
if ! command -v reportgenerator &> /dev/null; then
    echo "📥 Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Find all coverage files
COVERAGE_FILES=$(find TestResults/ -name "coverage.cobertura.xml" -type f)
if [ -z "$COVERAGE_FILES" ]; then
    echo "❌ No coverage files found!"
    exit 1
fi

# Convert to semicolon-separated list for ReportGenerator
COVERAGE_LIST=$(echo $COVERAGE_FILES | tr ' ' ';')

# Generate HTML report
reportgenerator \
    "-reports:$COVERAGE_LIST" \
    "-targetdir:../coveragereport" \
    "-reporttypes:Html;TextSummary;Badges;JsonSummary" \
    "-sourcedirs:." \
    "-historydir:../coveragehistory" \
    "-title:TodoApi Test Coverage Report" \
    "-tag:$(git rev-parse --short HEAD 2>/dev/null || echo 'local')"

# Generate coverage badge
echo "🏆 Generating coverage badge..."
COVERAGE_PERCENT=$(grep -o 'Line coverage: [0-9.]*%' ../coveragereport/Summary.txt | grep -o '[0-9.]*')
BADGE_COLOR="red"
if (( $(echo "$COVERAGE_PERCENT > 80" | bc -l) )); then
    BADGE_COLOR="green"
elif (( $(echo "$COVERAGE_PERCENT > 60" | bc -l) )); then
    BADGE_COLOR="yellow"
fi

# Create coverage summary for CI/CD
echo "📋 Creating coverage summary..."
cat > ../coveragereport/coverage-summary.json << EOF
{
  "coverage": $COVERAGE_PERCENT,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "commit": "$(git rev-parse --short HEAD 2>/dev/null || echo 'local')",
  "badge_url": "https://img.shields.io/badge/coverage-${COVERAGE_PERCENT}%25-${BADGE_COLOR}"
}
EOF

echo ""
echo "✅ Coverage Report Generated Successfully!"
echo "📊 Overall Coverage: ${COVERAGE_PERCENT}%"
echo "📂 Report Location: ../coveragereport/index.html"
echo "🌐 Open in browser: open ../coveragereport/index.html"
echo ""

# Display summary
if [ -f "../coveragereport/Summary.txt" ]; then
    echo "📈 Coverage Summary:"
    head -20 ../coveragereport/Summary.txt
fi

# Check coverage threshold (set to 75% as target)
THRESHOLD=75
if (( $(echo "$COVERAGE_PERCENT < $THRESHOLD" | bc -l) )); then
    echo "⚠️  Coverage ${COVERAGE_PERCENT}% is below threshold ${THRESHOLD}%"
    echo "💡 Consider adding more tests to improve coverage"
    exit 1
else
    echo "🎯 Coverage ${COVERAGE_PERCENT}% meets threshold ${THRESHOLD}%"
fi