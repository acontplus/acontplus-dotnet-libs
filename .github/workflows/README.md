# 🔄 GitHub Actions Workflows

Este directorio contiene workflows automatizados para CI/CD del monorepo Acontplus.

## 📁 Workflows Disponibles

### 🧠 **smart-publish.yml** ⭐⭐ INTELIGENTE - RECOMENDADO
**Detección automática y estrategia de publicación inteligente**

- ✅ **Análisis automático** de dependencias al mergear PR
- ✅ **Detecta** si paquetes tienen dependientes
- ✅ **Estrategia automática**:
  - Si tiene dependientes → Crea issue con recomendación de cascade
  - Si NO tiene dependientes → Publica directamente
- ✅ Tests automáticos
- ✅ Comentarios en PR con recomendaciones
- ✅ Zero configuración necesaria

**Trigger**: Automático al mergear cualquier PR que modifique `.csproj`

**Cómo funciona**:
1. Merges un PR con cambio de versión
2. Workflow analiza automáticamente las dependencias
3. Decide la estrategia apropiada
4. Ejecuta o recomienda la acción

---

### 🔄 **cascade-publish.yml**
**Publicación en cascada de paquetes NuGet con dependencias**

- ✅ Cálculo automático del grafo de dependencias
- ✅ Actualización secuencial en orden topológico
- ✅ Tests automáticos antes de publicar
- ✅ Creación de PR para review (recomendado)
- ✅ Verificación de disponibilidad en NuGet.org
- ✅ Changelog automático
- ✅ Rollback support con issues automáticos

**Uso**: Manual via GitHub UI → Actions → Cascade Publish (o cuando smart-publish lo recomienda)

**Documentación completa**: [CASCADE_PUBLISH_GUIDE.md](../../docs/CASCADE_PUBLISH_GUIDE.md)

---

### 🚀 **pr-cascade-publish.yml** ⭐ NUEVO
**Publicación automática al mergear PRs de cascade**

- ✅ Detecta merges de branches `cascade-update/*`
- ✅ Publica automáticamente a NuGet.org
- ✅ Ejecuta tests finales
- ✅ Crea GitHub Release
- ✅ Notificaciones de éxito/fallo

**Trigger**: Automático al mergear PR

---

### 📦 **nuget-publish.yml** ⚠️ LEGACY
**Publicación individual de paquetes (Solo manual)**

- ⚠️ **DESACTIVADO automáticamente** para evitar conflictos con smart-publish
- ✅ Solo para uso manual en emergencias
- ✅ Publicación paralela de múltiples paquetes
- ✅ Soporte para publicación forzada

**Trigger**: ~~Push a `main`~~ Solo Manual (workflow_dispatch)

**Nota**: Este workflow ha sido reemplazado por `smart-publish.yml` para operación normal. Se mantiene como fallback para casos de emergencia.

---

### ✅ **version-check.yml**
**Verificación de versiones publicadas**

- ✅ Compara versiones locales vs NuGet.org
- ✅ Identifica paquetes sin publicar
- ✅ Detecta paquetes nuevos
- ✅ Ejecución diaria automática

**Trigger**: Cron diario (9 AM UTC) o Manual

---

### 🏗️ **build-test.yml**
**Build y tests continuos**

- ✅ Build de toda la solución
- ✅ Ejecución de tests
- ✅ Validación de código

**Trigger**: Push y Pull Requests

---

## 🎯 Flujo de Trabajo Recomendado

### **Desarrollo Normal** ⭐ AUTOMÁTICO CON SMART-PUBLISH

1. **Creas feature branch** y haces cambios
   ```bash
   git checkout -b feature/nueva-funcionalidad
   # Editar código, actualizar versión en .csproj
   ```

2. **Create PR** → `build-test.yml` valida automáticamente

3. **Review y merge a main**

4. **`smart-publish.yml` se ejecuta automáticamente** y decide:

   **Escenario A: Paquete sin dependientes (ej: Barcode, S3Application)**
   ```
   ✅ Publica directamente a NuGet.org
   ✅ Crea GitHub Release
   ✅ Listo! ✨
   ```

   **Escenario B: Paquete con dependientes (ej: Core, Utilities)**
   ```
   ⚠️ Crea issue recomendando cascade update
   ⚠️ Comenta en tu PR con instrucciones
   ➡️ Debes ejecutar manualmente cascade-publish.yml
   ```

### **Actualización en Cascada** (Cuando smart-publish lo recomienda)

1. **Ejecutar `cascade-publish.yml`** manualmente:
   ```
   GitHub → Actions → Cascade Publish → Run workflow

   Parámetros:
   - Root Package: Core (o el paquete que actualizas)
   - Bump Type: minor/patch/major
   - Cascade Bump: patch (para dependientes)
   - Create PR: ✅ true (IMPORTANTE - permite review)
   - Run Tests: ✅ true
   - Dry Run: false
   ```

2. **Se crea PR automáticamente** con:
   - Cambios de versión en todos los paquetes dependientes
   - Changelog detallado
   - Labels: automated, version-bump, dependencies

3. **Review y merge** el PR

4. **`pr-cascade-publish.yml` publica automáticamente** al mergear

📖 **Documentación completa**: [CASCADE_PUBLISH_GUIDE.md](../../docs/CASCADE_PUBLISH_GUIDE.md)

### **Publicación Individual**

1. Actualizar versión en `.csproj` manualmente
2. Commit y push a `main`
3. `nuget-publish.yml` detecta y publica

---

## 🔒 Prevención de Conflictos

Los workflows están configurados para **evitar publicaciones duplicadas**:

### Mecanismos de Protección

1. **Concurrency Control**: smart-publish y pr-cascade-publish usan el mismo grupo de concurrencia
2. **Branch Detection**: smart-publish se salta automáticamente branches `cascade-update/*`
3. **Trigger Selectivo**: nuget-publish desactivado automáticamente (solo manual)
4. **Eligibility Check**: Verificación explícita antes de ejecutar

**Documentación completa**: [WORKFLOWS_CONFLICT_RESOLUTION.md](../../docs/WORKFLOWS_CONFLICT_RESOLUTION.md)

---

## Setup Instructions

### Prerequisites

1. **NuGet.org Account**: Create an account at [nuget.org](https://www.nuget.org)

2. **API Key**:
   - Go to [NuGet.org API Keys](https://www.nuget.org/account/apikeys)
   - Click "Create" and generate a new API key
   - Set permissions to "Push new packages and package versions"
   - Copy the API key (you won't be able to see it again)

3. **GitHub Repository Secret**:
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: Paste your NuGet.org API key
   - Click "Add secret"

### Configuration

The workflows are pre-configured for this monorepo structure:
- **Source projects**: `src/**/*.csproj`
- **.NET Version**: 10.0.x
- **Configuration**: Release
- **Output**: `nupkgs/` directory

#### Environment Variables

You can customize these in the workflow files:

```yaml
env:
  DOTNET_VERSION: '10.0.x'    # .NET SDK version
  CONFIGURATION: Release       # Build configuration
```

---

## Usage

### Automatic Publishing

1. **Update package version** in the `.csproj` file:
   ```xml
   <Version>2.1.0</Version>
   ```

2. **Commit and push** to `main`:
   ```bash
   git add src/Acontplus.Core/Acontplus.Core.csproj
   git commit -m "feat(core): add new feature"
   git push origin main
   ```

3. **GitHub Actions will**:
   - Detect the version change
   - Build and test the package
   - Publish to NuGet.org
   - Create a GitHub release

### Manual Publishing

Use the workflow dispatch for manual control:

1. Go to **Actions** → **Publish NuGet Packages** → **Run workflow**

2. Options:
   - **Packages**: Specify packages (comma-separated) or leave empty for all changed
     - Example: `Acontplus.Core, Acontplus.Utilities`
   - **Force**: Check to publish even if version exists (overwrites)

3. Click **Run workflow**

### Using the Version Upgrade Script

The repository includes a PowerShell script to automate version bumps:

```powershell
# Interactive mode (prompts for package and bump type)
.\upgrade-version.ps1

# Specify package and bump type
.\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor

# Skip build step
.\upgrade-version.ps1 -PackageName Acontplus.Utilities -BumpType patch -SkipBuild
```

**Bump Types**:
- `patch`: 2.0.3 → 2.0.4 (bug fixes)
- `minor`: 2.0.3 → 2.1.0 (new features, backward compatible)
- `major`: 2.0.3 → 3.0.0 (breaking changes)

After running the script, commit and push the changes to trigger automatic publishing.

---

## Workflow Behavior

### Version Detection

The workflow checks if a package version exists on NuGet.org:

- **New version**: Package is built and published
- **Existing version**: Skipped (unless force flag is used)
- **New package**: Automatically published

### Parallel Publishing

Multiple packages with version changes are published in parallel for faster deployment.

### Failure Handling

- **Individual package failure**: Other packages continue (fail-fast: false)
- **Build failure**: No packages are published
- **Publishing failure**: Artifacts are still uploaded to GitHub

---

## Monitoring

### Check Workflow Status

1. Go to **Actions** tab in your GitHub repository
2. Select the workflow run to see detailed logs
3. Review each job's output

### View Published Packages

- **NuGet.org**: `https://www.nuget.org/packages/[PackageName]`
- **GitHub Releases**: Repository → Releases
- **Workflow Artifacts**: Actions → Workflow run → Artifacts

### Notifications

GitHub will notify you (via email/web) when:
- Workflow completes successfully
- Workflow fails
- Manual approval is required (if configured)

---

## Troubleshooting

### Common Issues

#### 1. "401 Unauthorized" when publishing

**Solution**: Verify the `NUGET_API_KEY` secret is correctly set in GitHub repository settings.

#### 2. "409 Conflict - Package version already exists"

**Solution**: Bump the version number in the `.csproj` file. NuGet.org doesn't allow republishing the same version.

#### 3. Build errors during workflow

**Solution**:
- Run `dotnet build` locally to reproduce the issue
- Check .NET SDK version compatibility
- Verify all dependencies are available

#### 4. Workflow not triggering

**Solution**:
- Ensure changes are in `src/**/*.csproj` paths
- Check branch name matches trigger configuration
- Verify workflows are enabled in repository settings

### Debug Steps

1. **Check workflow logs**: Actions → Failed workflow → Job → Step
2. **Test locally**: Run the same commands on your machine
3. **Validate secrets**: Ensure `NUGET_API_KEY` is set correctly
4. **Check NuGet.org status**: Visit [status.nuget.org](https://status.nuget.org)

---

## Security Best Practices

✅ **DO**:
- Use GitHub Secrets for sensitive data (API keys)
- Limit API key permissions to "Push" only
- Regularly rotate API keys
- Use branch protection rules for `main`
- Review changes before merging to main

❌ **DON'T**:
- Commit API keys or secrets to the repository
- Use overly permissive API keys
- Publish from personal branches
- Skip version validation

---

## Advanced Configuration

### Publishing to Multiple Feeds

To publish to both NuGet.org and a private feed:

```yaml
- name: Publish to private feed
  run: |
    dotnet nuget push "nupkgs/*.nupkg" \
      --api-key ${{ secrets.PRIVATE_FEED_KEY }} \
      --source https://your-private-feed.com/nuget \
      --skip-duplicate

- name: Publish to NuGet.org
  run: |
    dotnet nuget push "nupkgs/*.nupkg" \
      --api-key ${{ secrets.NUGET_API_KEY }} \
      --source https://api.nuget.org/v3/index.json \
      --skip-duplicate
```

### Pre-release Versions

For pre-release versions, use semantic versioning:

```xml
<Version>2.1.0-beta.1</Version>
<Version>2.1.0-rc.2</Version>
```

The workflow automatically detects and publishes pre-release versions.

### Custom Release Notes

Modify the `create-release` job to customize release notes:

```powershell
$releaseNotes = "## What's New`n`n"
# Add custom content
```

---

## Support

For issues or questions:
- Review workflow logs in the Actions tab
- Check [GitHub Actions documentation](https://docs.github.com/actions)
- Consult [NuGet documentation](https://docs.microsoft.com/nuget/)

---

## Maintenance

### Regular Tasks

- **Monthly**: Review and rotate API keys
- **Per release**: Verify package quality on NuGet.org
- **As needed**: Update .NET SDK version in workflows
- **Quarterly**: Audit workflow permissions and security

### Updating Workflows

When modifying workflows:
1. Test changes in a feature branch
2. Use `workflow_dispatch` for manual testing
3. Monitor first runs carefully
4. Document changes in commit messages

---

## Related Files

- `upgrade-version.ps1`: Script to bump package versions
- `batch-upgrade-version.ps1`: Batch version upgrade script
- `Directory.Packages.props`: Central package version management
- `.github/instructions/commits.instructions.md`: Commit message guidelines
