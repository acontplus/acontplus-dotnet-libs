#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# SonarQube Local Scanner & API Results Exporter
# ==============================================================================

# Parse arguments and support flags like --export-only
TOKEN=""
SERVER_URL=""
PROJECT_KEY=""
PROJECT_NAME=""
AUTO_EXPORT=false
EXPORT_ONLY=false
USE_DOTNET_SCANNER=true

get_repository_name() {
  local remote_url repository_name

  remote_url=$(git config --get remote.origin.url 2>/dev/null || true)
  if [ -n "$remote_url" ]; then
    repository_name="${remote_url##*/}"
    repository_name="${repository_name##*:}"
    repository_name="${repository_name%.git}"
  else
    repository_name=$(basename "$(git rev-parse --show-toplevel 2>/dev/null || pwd)")
  fi

  printf '%s' "$repository_name"
}

for arg in "$@"; do
  case "$arg" in
    --export|-e)
      AUTO_EXPORT=true
      ;;
    --export-only|-x)
      EXPORT_ONLY=true
      AUTO_EXPORT=true
      ;;
    --cli)
      USE_DOTNET_SCANNER=false
      ;;
    *)
      if [ -z "$TOKEN" ]; then
        TOKEN="$arg"
      elif [ -z "$SERVER_URL" ]; then
        SERVER_URL="$arg"
      elif [ -z "$PROJECT_KEY" ]; then
        PROJECT_KEY="$arg"
      elif [ -z "$PROJECT_NAME" ]; then
        PROJECT_NAME="$arg"
      fi
      ;;
  esac
done

TOKEN="${TOKEN:-${SONAR_TOKEN:-}}"
SERVER_URL="${SERVER_URL:-${SONAR_HOST_URL:-http://localhost:9000}}"
REPOSITORY_NAME="$(get_repository_name)"
PROJECT_KEY="${PROJECT_KEY:-${SONAR_PROJECT_KEY:-$REPOSITORY_NAME}}"
PROJECT_NAME="${PROJECT_NAME:-$PROJECT_KEY}"
AUTO_EXPORT="${AUTO_EXPORT:-${EXPORT_SONAR_RESULTS:-false}}"

if [ -z "$TOKEN" ]; then
  echo "❌ Error: SonarQube token is required."
  echo ""
  echo "Uso: ./scripts/sonarqube-scan.sh <TOKEN> [SERVER_URL] [PROJECT_KEY] [PROJECT_NAME] [--export|--export-only]"
  echo "Ejemplo:"
  echo "  ./scripts/sonarqube-scan.sh squ_xxxxxxxxxxxx"
  echo "  ./scripts/sonarqube-scan.sh squ_xxxxxxxxxxxx --export"
  echo "  ./scripts/sonarqube-scan.sh squ_xxxxxxxxxxxx --export-only"
  echo "  ./scripts/sonarqube-scan.sh squ_xxxxxxxxxxxx http://localhost:9000 project-key 'My Project' --export"
  exit 1
fi

echo "=================================================="
echo "🚀 Iniciando SonarQube Scan local"
echo "🌐 Servidor: $SERVER_URL"
echo "🔑 Proyecto: $PROJECT_KEY"
echo "=================================================="

# 1. Verificar prerequisitos
echo -e "\n── Verificando prerequisitos"
command -v node >/dev/null 2>&1 || { echo "❌ Node.js no encontrado"; exit 1; }
echo "  ✓ Node.js $(node -v)"

# Scanner
export PATH="$PATH:$HOME/.dotnet/tools"

if [ "$EXPORT_ONLY" = "true" ]; then
  echo "  ↪ Modo exportación: se usarán los resultados existentes"
else
  if [ "$USE_DOTNET_SCANNER" = "true" ] && command -v dotnet-sonarscanner >/dev/null 2>&1 && command -v dotnet >/dev/null 2>&1; then
    DOTNET_SCANNER_VER=$( (dotnet-sonarscanner 2>&1 || true) | head -n 1 | tr -d '\r')
    echo "  ✓ .NET SDK $(dotnet --version)"
    echo "  ✓ Scanner tool: dotnet-sonarscanner ($DOTNET_SCANNER_VER)"
  else
    USE_DOTNET_SCANNER=false
    SCANNER_BIN=""
    if [ -f "./node_modules/.bin/sonar-scanner-npm" ]; then
      SCANNER_BIN="./node_modules/.bin/sonar-scanner-npm"
    elif command -v sonar-scanner >/dev/null 2>&1; then
      SCANNER_BIN="sonar-scanner"
    else
      SCANNER_BIN="npx @sonar/scan"
    fi
    echo "  ✓ Scanner tool: $SCANNER_BIN"
  fi
fi

# Check SonarQube server reachable
echo -n "  Verificando $SERVER_URL ... "
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/api/system/status" || echo "000")
if [ "$HTTP_STATUS" != "200" ]; then
  echo -e "\n❌ No se puede conectar a SonarQube en $SERVER_URL (HTTP $HTTP_STATUS)"
  echo "Asegúrate de que el contenedor esté corriendo y accesible."
  exit 1
fi
echo "OK ✓"

# Check Token
AUTH_HEADER="Authorization: Basic $(echo -n "${TOKEN}:" | base64 | tr -d '\r\n')"
VALID_RES=$(curl -s -H "$AUTH_HEADER" "$SERVER_URL/api/authentication/validate" || echo "{}")
IS_VALID=$(node -e "try { const d = JSON.parse(process.argv[1]); console.log(d.valid === true ? 'true' : 'false'); } catch { console.log('false'); }" "$VALID_RES")
if [ "$IS_VALID" != "true" ]; then
  echo "❌ Token inválido. Genera uno en $SERVER_URL → My Account → Security → Generate Tokens"
  exit 1
fi
echo "  ✓ Token autenticado correctamente"

if [ "$EXPORT_ONLY" != "true" ]; then
  # 2. Verificar o crear proyecto
  echo -e "\n── Verificando proyecto '$PROJECT_KEY' en SonarQube"
  SEARCH_RES=$(curl -s -H "$AUTH_HEADER" "$SERVER_URL/api/projects/search?projects=$PROJECT_KEY" || echo "{}")
  EXISTS=$(node -e "try { const d = JSON.parse(process.argv[1]); const exists = (d.components || []).some(c => c.key === '$PROJECT_KEY'); console.log(exists ? 'true' : 'false'); } catch { console.log('false'); }" "$SEARCH_RES")

  if [ "$EXISTS" = "true" ]; then
    echo "  ✓ Proyecto ya existe en SonarQube"
  else
    echo -n "  Creando proyecto '$PROJECT_KEY' ... "
    curl -s -X POST -H "$AUTH_HEADER" -d "project=$PROJECT_KEY&name=$(echo -n "$PROJECT_NAME" | python3 -c 'import sys, urllib.parse; print(urllib.parse.quote(sys.stdin.read()))')&mainBranch=main" "$SERVER_URL/api/projects/create" >/dev/null
    echo "OK ✓"
  fi

  # 3. Ejecutar Sonar Scanner
  REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
  EXCLUSIONS="**/*.png,**/*.jpg,**/*.jpeg,**/*.gif,**/*.ico,**/*.pdf,**/*.pfx,**/*.snk,**/*.dll,**/*.exe,**/*.zip,**/bin/**,**/obj/**,**/.sonarqube-results/**,**/nupkgs/**,**/TestResults/**,**/.agents/**"

  if [ "$USE_DOTNET_SCANNER" = "true" ]; then
    if [ -f "$REPO_ROOT/sonar-project.properties" ]; then
      echo "⚠️ Eliminando sonar-project.properties (incompatible con dotnet-sonarscanner)"
      rm -f "$REPO_ROOT/sonar-project.properties"
    fi

    echo -e "\n── Limpiando resultados de pruebas anteriores"
    rm -rf "$REPO_ROOT/TestResults"

    echo -e "\n── Ejecutando dotnet-sonarscanner begin"
    dotnet-sonarscanner begin \
      /k:"$PROJECT_KEY" \
      /n:"$PROJECT_NAME" \
      /d:sonar.host.url="$SERVER_URL" \
      /d:sonar.token="$TOKEN" \
      /d:sonar.scm.provider="git" \
      /d:sonar.sourceEncoding="UTF-8" \
      /d:sonar.projectBaseDir="$REPO_ROOT" \
      /d:sonar.exclusions="$EXCLUSIONS" \
      /d:sonar.cpd.exclusions="**/Migrations/**,**/tests/**,**/bin/**,**/obj/**,**/apps/**" \
      /d:sonar.coverage.exclusions="**/tests/**,**/Migrations/**,**/apps/**" \
      /d:sonar.cs.vstest.reportsPaths="TestResults/*.trx,**/*.trx" \
      /d:sonar.cs.cobertura.reportsPaths="TestResults/*.cobertura.xml,**/*.cobertura.xml" \
      /d:sonar.python.version="3"

    echo -e "\n── Compilando solución (dotnet build Release)"
    dotnet build "$REPO_ROOT/acontplus-dotnet-libs.slnx" --configuration Release

    echo -e "\n── Ejecutando pruebas unitarias con cobertura (dotnet test)"
    dotnet test --solution "$REPO_ROOT/acontplus-dotnet-libs.slnx" \
      --configuration Release \
      --no-build \
      --coverage \
      --coverage-output-format cobertura \
      --results-directory "$REPO_ROOT/TestResults" \
      --report-xunit-trx

    echo -e "\n── Ejecutando dotnet-sonarscanner end"
    dotnet-sonarscanner end /d:sonar.token="$TOKEN"
  else
    echo -e "\n── Ejecutando escaneo con Sonar Scanner CLI"
    $SCANNER_BIN \
      -Dsonar.host.url="$SERVER_URL" \
      -Dsonar.token="$TOKEN" \
      -Dsonar.projectKey="$PROJECT_KEY" \
      -Dsonar.projectName="$PROJECT_NAME" \
      -Dsonar.sourceEncoding="UTF-8" \
      -Dsonar.projectBaseDir="$REPO_ROOT" \
      -Dsonar.exclusions="$EXCLUSIONS" \
      -Dsonar.cpd.exclusions="**/Migrations/**,**/tests/**,**/bin/**,**/obj/**,**/apps/**" \
      -Dsonar.coverage.exclusions="**/tests/**,**/Migrations/**,**/apps/**" \
      -Dsonar.cs.vstest.reportsPaths="TestResults/*.trx,**/*.trx" \
      -Dsonar.cs.cobertura.reportsPaths="TestResults/*.cobertura.xml,**/*.cobertura.xml"
  fi

  echo "  ✓ Escaneo completado y enviado a SonarQube"

  # 4. Esperar análisis
  echo -e "\n── Esperando procesamiento en SonarQube"
  MAX_WAIT=120
  INTERVAL=5
  WAITED=0
  PROCESSED=false

  while [ $WAITED -lt $MAX_WAIT ]; do
    sleep $INTERVAL
    WAITED=$((WAITED + INTERVAL))
    echo -ne "  Esperando... ($WAITED/$MAX_WAIT s)\r"
    ANALYSIS_RES=$(curl -s -H "$AUTH_HEADER" "$SERVER_URL/api/project_analyses/search?project=$PROJECT_KEY&ps=1" || echo "{}")
    TOTAL=$(node -e "try { const d = JSON.parse(process.argv[1]); console.log(d.paging?.total || 0); } catch { console.log(0); }" "$ANALYSIS_RES")
    if [ "$TOTAL" -gt 0 ]; then
      PROCESSED=true
      break
    fi
  done

  echo ""
  if [ "$PROCESSED" != "true" ]; then
    echo "⚠️ El análisis está tardando en procesarse. Consulta en: $SERVER_URL/dashboard?id=$PROJECT_KEY"
    exit 0
  fi
  echo "  ✓ Análisis procesado exitosamente"
fi

# 5. Mostrar métricas
echo -e "\n── Métricas de Calidad del Proyecto"
METRICS="alert_status,bugs,vulnerabilities,code_smells,coverage,sqale_rating,reliability_rating,security_rating,ncloc,duplicated_lines,duplicated_blocks,duplicated_files,duplicated_lines_density,tests,test_errors,test_failures,test_success_density"
MEASURES_RES=$(curl -s -H "$AUTH_HEADER" "$SERVER_URL/api/measures/component?component=$PROJECT_KEY&metricKeys=$METRICS" || echo "{}")

node -e "
const d = JSON.parse(process.argv[1]);
const ratingMap = { '1.0': 'A ✅', '2.0': 'B 🟡', '3.0': 'C 🟠', '4.0': 'D 🔴', '5.0': 'E ⛔' };
const getRating = r => ratingMap[r] || r || 'N/A';
const m = {};
for (const measure of (d.component?.measures || [])) {
  m[measure.metric] = measure.value;
}
console.log('--------------------------------------------------');
console.log('📊 Líneas de Código:   ' + (m['ncloc'] || '0'));
console.log('🐛 Bugs:               ' + (m['bugs'] || '0') + ' (' + getRating(m['reliability_rating']) + ')');
console.log('🔓 Vulnerabilidades:   ' + (m['vulnerabilities'] || '0') + ' (' + getRating(m['security_rating']) + ')');
console.log('👃 Code Smells:        ' + (m['code_smells'] || '0') + ' (' + getRating(m['sqale_rating']) + ')');
if (m['tests']) {
  console.log('🧪 Pruebas Unitarias:  ' + m['tests'] + ' (éxito: ' + (m['test_success_density'] ? m['test_success_density'] + '%' : '100%') + ')');
}
console.log('🛡️ Cobertura:          ' + (m['coverage'] ? m['coverage'] + '%' : '0.0%'));
console.log('📄 Líneas Duplicadas:  ' + (m['duplicated_lines'] || '0'));
console.log('📐 Duplicación:        ' + (m['duplicated_lines_density'] ? m['duplicated_lines_density'] + '%' : '0%'));
console.log('🧱 Bloques Duplicados: ' + (m['duplicated_blocks'] || '0'));
console.log('📁 Archivos Afectados: ' + (m['duplicated_files'] || '0'));
console.log('🚦 Quality Gate:       ' + (m['alert_status'] === 'OK' ? 'PASSED ✅' : (m['alert_status'] || 'N/A')));
console.log('--------------------------------------------------');
" "$MEASURES_RES"

echo -e "\n🔗 Dashboard SonarQube: $SERVER_URL/dashboard?id=$PROJECT_KEY"

# 6. Preguntar y exportar resultados por API a .sonarqube-results
DO_EXPORT=false

if [ "$AUTO_EXPORT" = "true" ]; then
  DO_EXPORT=true
elif [ -t 0 ]; then
  echo ""
  read -r -p "📥 ¿Deseas exportar los resultados completos a .sonarqube-results? [S/n]: " USER_RESP
  USER_RESP="${USER_RESP:-S}"
  if [[ "$USER_RESP" =~ ^[SsYy]$ ]]; then
    DO_EXPORT=true
  fi
fi

if [ "$DO_EXPORT" = "true" ]; then
  DATE_FOLDER=$(date +"%Y-%m-%d_%H-%M-%S")
  EXPORT_DIR=".sonarqube-results/scans/${DATE_FOLDER}"
  mkdir -p "$EXPORT_DIR"
  echo -e "\n📦 Exportando resultados vía API a: $EXPORT_DIR"

  # Helper para guardar JSON formateado
  fetch_and_save() {
    local url="$1"
    local output_file="$2"
    curl -s -H "$AUTH_HEADER" "$url" | node -e "
      let data = '';
      process.stdin.on('data', chunk => data += chunk);
      process.stdin.on('end', () => {
        try {
          const parsed = JSON.parse(data);
          console.log(JSON.stringify(parsed, null, 2));
        } catch (e) {
          console.log(data);
        }
      });
    " > "$output_file"
  }

  echo -n "  ⏳ Descargando métricas (measures.json) ... "
  fetch_and_save "$SERVER_URL/api/measures/component?component=$PROJECT_KEY&metricKeys=$METRICS" "$EXPORT_DIR/measures.json"
  echo "OK ✓"

  echo -n "  ⏳ Descargando problemas (issues.json) ... "
  fetch_and_save "$SERVER_URL/api/issues/search?projects=$PROJECT_KEY&resolved=false&ps=500" "$EXPORT_DIR/issues.json"
  echo "OK ✓"

  echo -n "  ⏳ Descargando detalle de duplicaciones (duplications.json) ... "
  DUPLICATION_COMPONENTS_FILE="$EXPORT_DIR/.duplication-components.json"
  DUPLICATIONS_TMP_DIR=$(mktemp -d)
  fetch_and_save "$SERVER_URL/api/components/tree?component=$PROJECT_KEY&qualifiers=FIL&strategy=leaves&ps=500" "$DUPLICATION_COMPONENTS_FILE"

  DUPLICATION_INDEX=0
  while IFS= read -r COMPONENT_KEY; do
    DUPLICATION_INDEX=$((DUPLICATION_INDEX + 1))
    curl -sG -H "$AUTH_HEADER" \
      --data-urlencode "key=$COMPONENT_KEY" \
      "$SERVER_URL/api/duplications/show" \
      > "$DUPLICATIONS_TMP_DIR/${DUPLICATION_INDEX}.json"
  done < <(node -e "
    const fs = require('fs');
    const data = JSON.parse(fs.readFileSync(process.argv[1], 'utf8'));
    for (const component of data.components || []) {
      console.log(component.key);
    }
  " "$DUPLICATION_COMPONENTS_FILE")

  node - "$DUPLICATION_COMPONENTS_FILE" "$DUPLICATIONS_TMP_DIR" "$EXPORT_DIR/duplications.json" "$PROJECT_KEY" <<'NODE'
const fs = require('fs');
const path = require('path');

const componentsFile = process.argv[2];
const responsesDirectory = process.argv[3];
const outputFile = process.argv[4];
const projectKey = process.argv[5];
const components = JSON.parse(fs.readFileSync(componentsFile, 'utf8')).components || [];
const files = [];

for (let index = 0; index < components.length; index += 1) {
  const responseFile = path.join(responsesDirectory, `${index + 1}.json`);
  try {
    const response = JSON.parse(fs.readFileSync(responseFile, 'utf8'));
    if ((response.duplications || []).length > 0) {
      files.push({
        key: components[index].key,
        name: components[index].name,
        path: components[index].path,
        duplications: response.duplications
      });
    }
  } catch {
    continue;
  }
}

fs.writeFileSync(outputFile, JSON.stringify({ projectKey, files }, null, 2) + '\n');
NODE

  rm -rf "$DUPLICATIONS_TMP_DIR" "$DUPLICATION_COMPONENTS_FILE"
  echo "OK ✓"

  echo -n "  ⏳ Descargando Quality Gate (quality-gate.json) ... "
  fetch_and_save "$SERVER_URL/api/qualitygates/project_status?projectKey=$PROJECT_KEY" "$EXPORT_DIR/quality-gate.json"
  echo "OK ✓"

  echo -n "  ⏳ Descargando historial de análisis (latest-analysis.json) ... "
  fetch_and_save "$SERVER_URL/api/project_analyses/search?project=$PROJECT_KEY&ps=5" "$EXPORT_DIR/latest-analysis.json"
  echo "OK ✓"

  # Guardar metadata con contexto de Git
  GIT_COMMIT=$(git rev-parse HEAD 2>/dev/null || echo "N/A")
  GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "N/A")

  node -e "
    const fs = require('fs');
    const meta = {
      projectKey: process.argv[1],
      projectName: process.argv[2],
      serverUrl: process.argv[3],
      scanDate: new Date().toISOString(),
      git: {
        commit: process.argv[4],
        branch: process.argv[5]
      },
      exportPath: '$EXPORT_DIR'
    };
    fs.writeFileSync('$EXPORT_DIR/metadata.json', JSON.stringify(meta, null, 2) + '\n');
  " "$PROJECT_KEY" "$PROJECT_NAME" "$SERVER_URL" "$GIT_COMMIT" "$GIT_BRANCH"
  echo "  ✓ Generado metadata.json"

  # Crear / actualizar symlink .sonarqube-results/latest
  mkdir -p .sonarqube-results
  rm -f .sonarqube-results/latest
  ln -s "scans/${DATE_FOLDER}" .sonarqube-results/latest 2>/dev/null || true

  echo "=================================================="
  echo "✅ Resultados exportados exitosamente en:"
  echo "📁 $EXPORT_DIR"
  echo "📌 Acceso rápido: .sonarqube-results/latest"
  echo "=================================================="
fi

echo ""
