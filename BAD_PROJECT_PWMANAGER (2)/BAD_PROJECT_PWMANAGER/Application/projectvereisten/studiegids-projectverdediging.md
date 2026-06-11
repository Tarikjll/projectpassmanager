# Studiegids projectverdediging - PassManager

Deze gids helpt je om PassManager te verdedigen. De uitleg is gekoppeld aan jouw eigen code en aan de geziene theorie: Clean Architecture, MVC, routing, views/Razor, modelbinding, formulieren, Entity Framework Core, Dependency Injection, authenticatie en autorisatie.

## 0. Extra technieken buiten de cursus

Sommige onderdelen gaan verder dan wat letterlijk in de cursus `authenticatie en autorisatie` of de basislessen MVC behandeld werd. Dat is geen probleem, zolang je duidelijk kan uitleggen waarom je ze gebruikt hebt en hoe ze in je eigen code werken.

Belangrijkste extra technieken in dit project:

- **Culture cookie voor taalkeuze**: technisch nog steeds een gewone HTTP-cookie, maar met de standaard naam en waarde die ASP.NET Core localization automatisch kan lezen.
- **Centrale vertaalhelper `UiText`**: zichtbare teksten staan niet overal hardcoded in views, maar worden opgehaald met keys zoals `T("Account.LoginTitle")`.
- **AES-GCM encryptie voor opgeslagen wachtwoorden**: wachtwoorden moeten later opnieuw getoond/geexporteerd kunnen worden, dus ze worden versleuteld en niet enkel gehasht.
- **HMAC-SHA256 fingerprint voor hergebruikdetectie**: hiermee kan de app zien of hetzelfde wachtwoord meerdere keren gebruikt wordt zonder het wachtwoord in plain text te vergelijken.
- **Cryptografisch veilige generator**: wachtwoorden worden gegenereerd met `RandomNumberGenerator`, niet met gewone `Random`.
- **Key provider**: encryptie- en HMAC-keys kunnen uit configuratie of environment variables komen, zodat secrets niet hardcoded moeten blijven in productie.
- **Legacy decrypt fallback**: oude records die nog in het vorige formaat opgeslagen zijn, kunnen nog gelezen worden.

Verdedigingszin:

> De cursus behandelt de basis van cookies, authenticatie, autorisatie en beveiliging. Ik heb daarop verder gebouwd met de standaard ASP.NET Core localization-cookie voor taalkeuze en met cryptografische technieken zoals AES-GCM en HMAC-SHA256, omdat een password manager gevoelige data verwerkt en meer beveiliging nodig heeft dan gewone CRUD.

### Culture cookie vs. gewone cookie

In de cursus zie je waarschijnlijk gewone cookies zoals:

```csharp
Response.Cookies.Append("language", "nl-BE");
```

In mijn project gebruik ik ook een gewone browsercookie, maar via de helper van ASP.NET Core:

```csharp
Response.Cookies.Append(
    CookieRequestCultureProvider.DefaultCookieName,
    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
    new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    });
```

Het verschil zit dus niet in het cookie-principe. Het verschil is dat ASP.NET Core localization deze cookie automatisch herkent. Daardoor moet ik niet bij elke request zelf een cookie lezen en de taal instellen.

Belangrijke punten:

- De cookie staat nog steeds in de browser.
- De cookie onthoudt enkel de taalvoorkeur, bijvoorbeeld `nl-BE` of `en`.
- Dit is niet dezelfde cookie als de Identity login-cookie.
- `UseRequestLocalization` leest deze culture cookie en zet `CultureInfo.CurrentUICulture`.
- `UiText.T(...)` gebruikt daarna die actieve culture om Nederlands of Engels terug te geven.

Verdedigingszin:

> Een culture cookie is eigenlijk een gewone cookie met een standaard ASP.NET Core-formaat. Ik gebruikte die omdat `UseRequestLocalization` ze automatisch uitleest en de juiste culture instelt voor de hele request.

### Hoe de vertaalfunctionaliteit gemaakt is

De vertaling bestaat uit vier delen.

1. **Culture configuratie in `Program.cs`**

In `Program.cs` worden de ondersteunde talen geregistreerd:

```csharp
var supportedCultures = new[]
{
    new CultureInfo("nl-BE"),
    new CultureInfo("nl"),
    new CultureInfo("en")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl-BE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
```

Hiermee weet ASP.NET Core welke talen geldig zijn en wat de standaardtaal is.

2. **Taal wijzigen via `HomeController.SetLanguage`**

De gebruiker kiest een taal in de navbar. Die keuze gaat naar:

- `BAD_PROJECT_PWMANAGER/Controllers/HomeController.cs`
- actie `SetLanguage(string culture, string returnUrl)`

Die actie:

- controleert of de gekozen culture toegestaan is;
- schrijft de culture cookie;
- redirect terug naar de oorspronkelijke pagina;
- gebruikt `Url.IsLocalUrl(returnUrl)` zodat de redirect niet naar een externe gevaarlijke URL kan gaan.

3. **Teksten centraal in `UiText.cs`**

In `BAD_PROJECT_PWMANAGER/UiText.cs` staan twee dictionaries:

- `Dutch`
- `English`

Elke tekst heeft een key:

```csharp
["Account.LoginTitle"] = "Aanmelden"
["Account.RegisterTitle"] = "Account maken"
["Password.Export"] = "Exporteren"
```

De methode `T(...)` kijkt naar `CultureInfo.CurrentUICulture`:

- bij Engels gebruikt ze de Engelse dictionary;
- anders gebruikt ze Nederlands als fallback;
- als een key ontbreekt, wordt de key zelf getoond, zodat een ontbrekende vertaling snel zichtbaar is.

4. **Gebruik in Razor views en Identity pages**

In `_ViewImports.cshtml` staat:

```cshtml
@using static BAD_PROJECT_PWMANAGER.UiText
```

Daardoor kan elke view rechtstreeks dit gebruiken:

```cshtml
<h1>@T("Account.LoginTitle")</h1>
<button>@T("Common.Save")</button>
```

Ook de gescaffoldde Identity pages zoals login en register gebruiken dit. De scaffolded pages zijn dus niet weggegooid. Ze zijn aangepast zodat hun zichtbare tekst via `T("...")` komt in plaats van rechtstreeks in de `.cshtml` te staan.

Waarom deze aanpak?

- Het is eenvoudiger dan overal hardcoded Nederlands en Engels te mengen.
- De layout en de tekst zijn gescheiden.
- Een vertaling aanpassen gebeurt op een centrale plaats.
- De taalkeuze blijft bewaard na refresh of opnieuw openen van de browser.
- Het sluit aan bij ASP.NET Core localization zonder dat elke view zelf cookies moet lezen.

Eerlijke nuance:

- Er bestaan ook `.resx` resource files in het project.
- In de huidige UI wordt vooral `UiText.cs` gebruikt voor de zichtbare appteksten.
- Sommige validatiemeldingen in ViewModels/InputModels staan nog als DataAnnotations in de code.
- Voor een groter productieproject zou je alles centraler kunnen migreren naar `.resx`, maar voor dit project is `UiText` overzichtelijk en makkelijk te verdedigen.

## 1. Korte projectpitch

PassManager is een .NET 8 MVC-applicatie waarmee gebruikers wachtwoorden veilig kunnen beheren in Vaults. De applicatie gebruikt ASP.NET Core Identity voor login en rollen. Free users hebben limieten, Premium users krijgen extra securityfuncties en Admins beheren gebruikersrollen en auditlogs.

Sterke punten om te vermelden:

- Niet enkel CRUD: er zit businesslogica in limieten, wachtwoordsterkte, encryptie, hashing, generator, geschiedenis, export en audit logging.
- Minstens drie gelinkte entiteiten: `Vault`, `PasswordEntry`, `PasswordHistory`, `PasswordExport`, `AuditLog`.
- Minstens twee rollen: je gebruikt drie rollen (`Admin`, `FreeUser`, `PremiumUser`).
- Clean Architecture structuur met aparte projecten `Domain`, `Application`, `Infrastructure` en `BAD_PROJECT_PWMANAGER` als MVC/WebUI.
- EF Core met SQL Server LocalDB.
- Beveiliging via Identity, `[Authorize]`, `[ValidateAntiForgeryToken]`, validatie-attributen, AES-GCM encryptie en wachtwoordcontrole bij Premium-export.

## 2. Vereisten checklist

| Vereiste | Waar zit dit in jouw project? |
| --- | --- |
| .NET 8 MVC-applicatie | `BAD_PROJECT_PWMANAGER.csproj`, controllers en Razor views |
| Clean Architecture | Projecten `Domain`, `Application`, `Infrastructure`, `BAD_PROJECT_PWMANAGER` |
| EF Core + MS SQL Server | `Program.cs`, `ApplicationDbContext`, `appsettings.json` |
| Min. 3 gelinkte entiteiten | `Vault`, `PasswordEntry`, `PasswordHistory`, `PasswordExport`, `AuditLog` |
| Businesslogica | `VaultService`, `PasswordEntryService`, security services, generator |
| Min. 2 rollen | `Admin`, `FreeUser`, `PremiumUser` |
| Authenticatie | ASP.NET Core Identity pages |
| Autorisatie | `[Authorize]`, `[Authorize(Roles = "...")]` |
| CSRF bescherming | `[ValidateAntiForgeryToken]` op POST-acties |
| Validatie | DataAnnotations in ViewModels en Identity inputmodels |
| Nette layout | Razor views, Bootstrap, custom CSS, Premium dark mode |

## 3. Architectuur uitleggen

Geziene theorie: Clean Architecture, repository, services, Unit of Work.

Jouw project volgt deze lagen:

- `Domain`: bevat de kernentiteiten, bijvoorbeeld `Vault`, `PasswordEntry`, `PasswordHistory`, `AuditLog`, `ApplicationUser`.
- `Application`: bevat interfaces en services met businesslogica, bijvoorbeeld `IVaultService`, `IPasswordEntryService`, `VaultService`, `PasswordEntryService`.
- `Infrastructure`: bevat technische implementaties zoals EF Core `ApplicationDbContext`, repositories en Unit of Work.
- `Application`: bevat naast interfaces ook de concrete business/security services zoals encryptie, hashing, strength-check en generator.
- `BAD_PROJECT_PWMANAGER`: is de MVC-presentatielaag met controllers, views, Identity pages, ViewModels en `Program.cs`.

Verdedigingszin:

> Ik heb de applicatie opgesplitst volgens Clean Architecture. De domeinmodellen zitten in `Domain`, de businesslogica zit in `Application`, technische details zoals EF Core en security services zitten in `Infrastructure`, en de MVC UI zit in `BAD_PROJECT_PWMANAGER`. Daardoor blijft de businesslogica beter gescheiden van de webinterface en de database.

## 4. Repository en Unit of Work

Geziene theorie: Repository abstraheert datatoegang; Unit of Work zorgt dat `SaveChanges()` centraal gebeurt.

In jouw code:

- `Application/Interfaces/IRepository.cs`
- `Application/Interfaces/IUnitOfWork.cs`
- `Infrastructure/Repositories/Repository.cs`
- `Infrastructure/Repositories/UnitOfWork.cs`

Hoe het werkt:

- `Repository<T>` bevat algemene methodes zoals `GetAll`, `GetById`, `Add`, `Update`, `Delete`.
- `UnitOfWork` bevat repositories voor `Vaults`, `PasswordEntries`, `PasswordHistories`, `PasswordExports` en `AuditLogs`.
- Services gebruiken `_unitOfWork` en roepen pas aan het einde `_unitOfWork.Save()` aan.

Voorbeeld:

- `VaultService.CreateVault` voegt een `Vault` toe.
- Dezelfde methode voegt ook een `AuditLog` toe.
- Daarna volgt een enkele `_unitOfWork.Save()`.

Verdedigingszin:

> Mijn repositories doen geen `SaveChanges()` per operatie. De `UnitOfWork` centraliseert dat via `Save()`, zodat meerdere wijzigingen samen bewaard worden.

## 5. Entity Framework Core en database

Geziene theorie: DbContext, DbSet, migrations, SQL Server.

In jouw code:

- `Infrastructure/Data/ApplicationDbContext.cs`
- `Infrastructure/Migrations`
- `BAD_PROJECT_PWMANAGER/appsettings.json`
- `BAD_PROJECT_PWMANAGER/Program.cs`

Belangrijk:

- `ApplicationDbContext` erft van `IdentityDbContext<ApplicationUser>`.
- Daardoor combineer je eigen entiteiten met Identity-tabellen.
- Je connectie gebruikt `DefaultConnection`.
- De gebruikte database is `BAD_PROJECT_PWMANAGER_DB` op `(localdb)\mssqllocaldb`.

DbSets:

- `Vaults`
- `PasswordEntries`
- `PasswordHistories`
- `PasswordExports`
- `AuditLogs`

Verdedigingszin:

> Mijn DbContext erft van `IdentityDbContext<ApplicationUser>`, waardoor ASP.NET Identity en mijn eigen tabellen in dezelfde SQL Server database zitten.

## 6. MVC, controllers en routing

Geziene theorie: routing mapt URL naar controller/action.

In `Program.cs` staat conventionele routing:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Voorbeelden:

- `/Vault/Index` gaat naar `VaultController.Index`.
- `/PasswordEntry/Details/5` gaat naar `PasswordEntryController.Details(int id)`.
- `/PasswordGenerator` gaat naar `PasswordGeneratorController.Index`.
- `/Admin/Users` gaat naar `AdminController.Users`.

Controllers:

- `HomeController`: dashboard, taalwissel, privacy.
- `VaultController`: CRUD voor Vaults.
- `PasswordEntryController`: wachtwoorden beheren, details, export, history.
- `PasswordGeneratorController`: premium generator.
- `AdminController`: gebruikersbeheer en auditlogs.
- `ProfileImageController`: profielfoto ophalen.

Verdedigingszin:

> De routing gebruikt de standaard MVC-conventie `{controller=Home}/{action=Index}/{id?}`. Daardoor wordt een URL automatisch vertaald naar een controller, action en optionele id-parameter.

## 7. Views, Razor en ViewModels

Geziene theorie: views genereren HTML via Razor; ViewModels geven gecontroleerde data door.

In jouw code:

- Views staan in `BAD_PROJECT_PWMANAGER/Views`.
- Identity Razor Pages staan in `BAD_PROJECT_PWMANAGER/Areas/Identity/Pages`.
- ViewModels staan in `BAD_PROJECT_PWMANAGER/ViewModels`.

Voorbeelden van ViewModels:

- `VaultFormViewModel`
- `VaultListItemViewModel`
- `PasswordEntryFormViewModel`
- `PasswordEntryDetailViewModel`
- `PasswordEntryListItemViewModel`
- `PasswordGeneratorViewModel`
- `UserListViewModel`
- `AuditLogListItemViewModel`

Waarom ViewModels?

- Je toont niet rechtstreeks alle entity-properties in de view.
- Je kan validatie toevoegen met DataAnnotations.
- Je kan berekende velden toevoegen zoals `IsPasswordOld`, `IsPasswordReused`, `StrengthLabel`.

Voorbeeld:

- `PasswordEntry` bevat `EncryptedPassword`.
- `PasswordEntryDetailViewModel` bevat een tijdelijk gedecrypteerde `Password` voor de details-view.

Verdedigingszin:

> Ik gebruik ViewModels zodat de view enkel de data krijgt die nodig is voor het scherm. Dat is veiliger en overzichtelijker dan rechtstreeks met database-entiteiten in de view te werken.

## 8. Modelbinding en formulieren

Geziene theorie: modelbinding vult parameters of ViewModels op basis van form data, route values en querystring.

Voorbeelden:

- `VaultController.Create(VaultFormViewModel model)`
- `VaultController.Edit(VaultFormViewModel model)`
- `PasswordEntryController.Create(PasswordEntryFormViewModel model)`
- `PasswordEntryController.Edit(PasswordEntryFormViewModel model)`
- `PasswordGeneratorController.Index(PasswordGeneratorViewModel model)`
- `AdminController.SetRole(string userId, string role)`
- `HomeController.SetLanguage(string culture, string returnUrl)`

Validatie:

- `ModelState.IsValid` controleert DataAnnotations.
- `[Required]`, `[StringLength]`, `[Url]`, `[Range]`, `[Compare]`, `[Phone]`, `[RegularExpression]`.

Voorbeeld:

- `PasswordEntryFormViewModel.PlainPassword` heeft `[Required]` en `[DataType(DataType.Password)]`.
- `PasswordGeneratorViewModel.Length` heeft `[Range(8, 128)]`.
- Register controleert dubbele gebruikersnaam en dubbele e-mail.

Verdedigingszin:

> Bij POST-acties laat ik ASP.NET Core de form data binden naar ViewModels. Daarna controleer ik `ModelState.IsValid` voordat ik businesslogica uitvoer.

## 9. Dependency Injection

Geziene theorie: services worden geregistreerd in `Program.cs` en geinjecteerd via constructors.

Registratie in `Program.cs`:

- `AddDbContext<ApplicationDbContext>`
- `AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>()`
- `AddScoped<IUnitOfWork, UnitOfWork>()`
- `AddScoped<IVaultService, VaultService>()`
- `AddScoped<IPasswordEntryService, PasswordEntryService>()`
- `AddScoped<IPasswordGeneratorService, PasswordGeneratorService>()`
- `AddScoped<IPasswordEncryptionService, PasswordEncryptionService>()`
- `AddScoped<IPasswordHashService, PasswordHashService>()`
- `AddScoped<IPasswordStrengthService, PasswordStrengthService>()`

Voorbeelden van injectie:

- `VaultController` krijgt `IVaultService` en `UserManager<ApplicationUser>`.
- `PasswordEntryController` krijgt entry-, strength- en encryption-services.
- `AdminController` krijgt `UserManager<ApplicationUser>` en `IUnitOfWork`.
- `PasswordEntryService` krijgt `IUnitOfWork`, encryptie, hashing en strength service.

Verdedigingszin:

> Ik gebruik Dependency Injection zodat controllers afhankelijk zijn van interfaces of frameworkservices in plaats van zelf concrete objecten aan te maken. Dat maakt de code losser gekoppeld en beter testbaar.

## 10. Authenticatie en autorisatie

Geziene theorie: authenticatie bepaalt wie je bent; autorisatie bepaalt wat je mag.

Authenticatie:

- ASP.NET Core Identity.
- Login/register pages in `Areas/Identity/Pages/Account`.
- `ApplicationUser` breidt `IdentityUser` uit met profielvelden.

Autorisatie:

- `[Authorize]` op `VaultController` en `PasswordEntryController`.
- `[Authorize(Roles = "PremiumUser")]` op `PasswordGeneratorController`.
- `[Authorize(Roles = "PremiumUser")]` op `PasswordEntryController.Export`.
- `[Authorize(Roles = "Admin")]` op `AdminController`.

Rollen:

- Rollen worden geseed in `Program.cs`.
- Nieuwe gebruikers worden `FreeUser`.
- Admin kan rollen wijzigen.

Verdedigingszin:

> Authenticatie gebeurt via ASP.NET Core Identity. Autorisatie gebeurt met role-based authorization via `[Authorize(Roles = "...")]`.

## 11. Beveiliging

Belangrijke beveiligingsmaatregelen:

- Identity password policy in `Program.cs`.
- Role-based authorization.
- `[ValidateAntiForgeryToken]` op POST-acties tegen CSRF.
- DataAnnotations-validatie.
- Eigenarencontrole: services halen Vaults en entries altijd op via `userId`.
- Wachtwoorden worden versleuteld opgeslagen via AES-GCM met random nonce en authentication tag.
- Oude wachtwoorden die nog met de vorige AES-CBC-opslag zijn bewaard, kunnen nog gelezen worden via een legacy decrypt fallback.
- Wachtwoordhash wordt gebruikt om hergebruik te detecteren zonder plain text te vergelijken.
- Die hash is nu een HMAC-SHA256 fingerprint met pepper/key, geen gewone SHA-256 meer.
- Export is Premium-only en vereist opnieuw het accountwachtwoord via `CheckPasswordAsync`.
- Upload van profielfoto controleert content type en max. 2 MB.

Belangrijke code:

- `PasswordEncryptionService`
- `PasswordHashService`
- `SecurityKeyProvider`
- `PasswordStrengthService`
- `PasswordEntryController.Export`
- `Areas/Identity/Pages/Account/Manage/Index.cshtml.cs`

Verdedigingszin:

> Nieuwe opgeslagen wachtwoorden worden versleuteld met AES-GCM. Dat geeft niet alleen vertrouwelijkheid, maar ook integriteitscontrole via de authentication tag. Voor hergebruikdetectie gebruik ik geen plain SHA-256 meer, maar HMAC-SHA256 met een geheime pepper, zodat dezelfde wachtwoorden vergelijkbaar blijven zonder de ruwe wachtwoorden te bewaren.

Waarom geen gewone salt op `PasswordHash`?

- Een normale unieke salt per wachtwoord zou ervoor zorgen dat hetzelfde wachtwoord telkens een andere hash krijgt.
- Dan kan de app hergebruikte wachtwoorden niet meer herkennen door hashes te groeperen.
- Daarom gebruikt deze app voor duplicate detection een keyed HMAC-fingerprint.
- Accountwachtwoorden zelf worden niet door deze service gehasht; dat doet ASP.NET Core Identity met zijn eigen salted password hasher.

Configuratie:

- `PASSMANAGER_ENCRYPTION_KEY` of `PasswordSecurity:EncryptionKey` voor encryptie.
- `PASSMANAGER_HASH_PEPPER` of `PasswordSecurity:HashPepper` voor HMAC.
- `SecurityKeyProvider` kan base64 of gewone tekst verwerken en normaliseert naar een 32-byte key.

### In detail: waarom encryptie?

Een wachtwoordmanager moet opgeslagen wachtwoorden later opnieuw kunnen tonen aan de eigenaar. Bijvoorbeeld:

- in `PasswordEntryController.Details`;
- bij Premium CSV-export;
- bij wachtwoordgeschiedenis.

Daarom is gewone hashing niet genoeg. Een hash is eenrichtingsverkeer: je kan een hash niet terug omzetten naar het originele wachtwoord. Voor accountwachtwoorden is dat goed, want je moet alleen controleren of de login klopt. Voor opgeslagen websitewachtwoorden is dat niet voldoende, want de gebruiker moet het wachtwoord later opnieuw kunnen lezen.

Daarom gebruikt `PasswordEncryptionService` encryptie:

- `Encrypt(plainText)` zet het leesbare wachtwoord om naar ciphertext.
- `Decrypt(encryptedText)` zet de ciphertext terug om naar plain text wanneer de eigenaar het nodig heeft.

Verdedigingszin:

> Accountwachtwoorden worden door ASP.NET Core Identity gehasht, maar opgeslagen websitewachtwoorden moeten later opnieuw leesbaar zijn voor de gebruiker. Daarom gebruik ik voor die data encryptie in plaats van enkel hashing.

### In detail: waarom AES-GCM?

De huidige opslag gebruikt prefix `v2:` en AES-GCM.

AES-GCM is gekozen omdat het authenticated encryption is:

- het versleutelt de inhoud;
- het maakt een authentication tag;
- bij decryptie wordt gecontroleerd of de data niet aangepast is.

De service gebruikt:

- een random nonce van 12 bytes;
- een tag van 16 bytes;
- een 32-byte key;
- base64 om het resultaat als string in de database op te slaan.

De opslagvorm is:

```text
v2:{base64(nonce + tag + ciphertext)}
```

Waarom een random nonce?

- Als twee gebruikers toevallig hetzelfde wachtwoord opslaan, mag de ciphertext niet voorspelbaar hetzelfde worden.
- De nonce zorgt dat dezelfde plain text telkens anders versleuteld wordt.

Waarom een prefix `v2:`?

- Zo weet de decryptiemethode welk formaat gebruikt is.
- Nieuwe records gebruiken AES-GCM.
- Oude records zonder prefix worden via de legacy decrypt fallback gelezen.

Verdedigingszin:

> AES-GCM is sterker dan enkel AES-CBC omdat het niet alleen versleutelt, maar ook controleert of de versleutelde data niet aangepast werd. De `v2:` prefix maakt het mogelijk om oude en nieuwe opslagformaten naast elkaar te ondersteunen.

### In detail: legacy decrypt fallback

Eerder gebruikte de app een ouder AES-formaat. Om bestaande data niet onleesbaar te maken, bevat `PasswordEncryptionService.Decrypt(...)` twee paden:

- start de string met `v2:`, dan gebruikt de app de nieuwe AES-GCM decryptie;
- anders gebruikt de app `DecryptLegacy(...)`.

Dit is vooral praktisch bij evolutie van een project:

- bestaande wachtwoorden blijven bruikbaar;
- nieuwe wachtwoorden worden veiliger opgeslagen;
- later kan je eventueel een migratie schrijven die oude records opnieuw versleutelt naar `v2:`.

Verdedigingszin:

> Ik heb backwards compatibility voorzien. Oude records worden nog gelezen via een legacy decrypt pad, terwijl nieuwe records automatisch in het nieuwe AES-GCM-formaat opgeslagen worden.

### In detail: waarom hashing/HMAC naast encryptie?

Encryptie lost het tonen/exporteren van wachtwoorden op, maar niet het veilig vergelijken van wachtwoorden voor hergebruikdetectie.

Voor Premium users wil de app kunnen zeggen:

- dit wachtwoord wordt ook gebruikt bij een andere entry;
- toon welke platformen hetzelfde wachtwoord gebruiken.

Daarom bewaart `PasswordHashService` een HMAC-SHA256 fingerprint in `PasswordEntry.PasswordHash`.

Belangrijk verschil:

- **Encryptie**: om het wachtwoord later terug te kunnen lezen.
- **HMAC/hash fingerprint**: om wachtwoorden te kunnen vergelijken zonder ze te decrypten of plain text te bewaren.

Waarom geen gewone SHA-256?

- Gewone SHA-256 is niet keyed.
- Als iemand de database heeft, kan die veel voorkomende wachtwoorden zelf hashen en vergelijken.
- HMAC-SHA256 gebruikt een geheime pepper/key, waardoor de fingerprint zonder die key veel minder bruikbaar is.

Waarom geen unieke salt per opgeslagen wachtwoord?

- Een unieke salt maakt dezelfde wachtwoorden expres verschillend.
- Dat is goed voor accountwachtwoorden, maar slecht voor duplicate detection.
- Als dezelfde plain text altijd een andere hash krijgt, kan je hergebruik niet detecteren.
- Daarom gebruikt deze app een vaste geheime HMAC-key/pepper voor vergelijkbare fingerprints.

Verdedigingszin:

> Voor hergebruikdetectie heb ik een stabiele maar geheime fingerprint nodig. Een gewone unieke salt zou duplicate detection onmogelijk maken, daarom gebruik ik HMAC-SHA256 met een geheime pepper.

### In detail: key management

`SecurityKeyProvider` zorgt dat keys niet verspreid staan in de code.

Voor encryptie:

- configuratiekey: `PasswordSecurity:EncryptionKey`;
- environment variable: `PASSMANAGER_ENCRYPTION_KEY`.

Voor HMAC:

- configuratiekey: `PasswordSecurity:HashPepper`;
- environment variable: `PASSMANAGER_HASH_PEPPER`.

De provider kan:

- een base64-string lezen;
- gewone tekst lezen;
- de input normaliseren naar 32 bytes met SHA-256 als de lengte niet exact klopt.

Waarom environment variables?

- In productie wil je secrets niet hardcoded in Git of in gewone configbestanden.
- Met environment variables kan de server de echte secret leveren.
- De fallback is handig voor lokale ontwikkeling, maar niet ideaal voor productie.

Verdedigingszin:

> De key provider maakt het mogelijk om lokaal met een fallback te werken, maar in productie echte secrets via environment variables of user secrets te gebruiken.

### In detail: cryptografisch veilige generator

De password generator gebruikt `RandomNumberGenerator.GetInt32(...)`.

Waarom niet `Random`?

- `Random` is bedoeld voor algemene willekeur, niet voor security.
- Wachtwoorden moeten moeilijk voorspelbaar zijn.
- `RandomNumberGenerator` komt uit `System.Security.Cryptography` en is geschikt voor securitygevoelige random waarden.

De generator:

- controleert dat de lengte tussen 8 en 128 ligt;
- bouwt de toegestane tekensets op basis van de gekozen opties;
- zorgt dat elk gekozen type minstens een keer voorkomt;
- vult de rest random aan;
- shufflet de tekens cryptografisch veilig.

Verdedigingszin:

> Omdat dit wachtwoorden zijn, gebruik ik geen gewone `Random`, maar `RandomNumberGenerator`. Daardoor is de gegenereerde output veel minder voorspelbaar.

## 12. Businesslogica die verder gaat dan CRUD

Belangrijke voorbeelden:

- FreeUser mag maximaal 3 Vaults aanmaken.
- FreeUser mag maximaal 10 wachtwoorden opslaan.
- PremiumUser heeft onbeperkte limieten.
- PremiumUser krijgt wachtwoordgeschiedenis.
- PremiumUser krijgt detectie van hergebruikte wachtwoorden.
- PremiumUser krijgt detectie van oude wachtwoorden na 90 dagen.
- Password generator gebruikt gekozen opties en cryptografische randomisatie.
- Wachtwoordsterkte wordt berekend op basis van lengte, hoofdletters, kleine letters, cijfers en symbolen.
- Export is Premium-only en vereist het accountwachtwoord.
- Admin-acties worden gelogd.

Belangrijke code:

- `VaultService`
- `PasswordEntryService`
- `PasswordGeneratorService`
- `PasswordStrengthService`
- `AdminController`

Verdedigingszin:

> De app is niet enkel CRUD. De belangrijkste businesslogica zit in de services: limieten per rol, security checks, wachtwoordgeschiedenis, sterkteberekening, encryptie, hashing en audit logging.

## 13. Taal en thema

Extra UI-functionaliteiten:

- Light theme voor bezoekers en FreeUser.
- Dark mode als Premium/Admin feature via CSS en JavaScript.
- Taalkeuze via cookie en `RequestLocalization`.
- UI helper `UiText` voor NL/EN teksten.
- Navbar toont actieve taal als `BE` of `GB`.

Belangrijke code:

- `Program.cs`
- `HomeController.SetLanguage`
- `UiText.cs`
- `Views/Shared/_Layout.cshtml`
- `wwwroot/css/site.css`

Verdedigingszin:

> De taal wordt bewaard in een culture-cookie. De layout gebruikt de huidige culture om de juiste tekst te tonen. Dark mode is bewust als Premium feature gemaakt: de layout toont de theme toggle alleen voor PremiumUser en Admin, en JavaScript forceert light mode voor andere gebruikers.

### Hoe vertaling in deze app werkt

De app gebruikt een eenvoudige centrale vertaalhelper in `BAD_PROJECT_PWMANAGER/UiText.cs`.

Werking:

- `UiText` bevat twee dictionaries: een Nederlandse dictionary en een Engelse dictionary.
- Elke zichtbare tekst krijgt een vaste key, bijvoorbeeld `Home.PremiumHeading`, `Password.New` of `Account.LoginTitle`.
- De methode `UiText.Get(...)` kijkt naar `CultureInfo.CurrentUICulture`.
- Is de actieve culture Engels, dan gebruikt de app de Engelse dictionary.
- In alle andere gevallen gebruikt de app Nederlands als fallback.
- De methode `UiText.T(...)` is een korte wrapper rond `Get(...)` en gebruikt automatisch de actieve culture.
- Via `_ViewImports.cshtml` is `@T("Key")` beschikbaar in alle normale MVC views en Identity Razor Pages.

Voorbeeld in een Razor view:

```cshtml
<h1>@T("Password.Title")</h1>
<button class="btn btn-primary">@T("Common.Save")</button>
```

De actieve taal wordt ingesteld via `HomeController.SetLanguage`. Die actie schrijft een cookie met de gekozen culture:

- `nl-BE` voor Nederlands/Belgie.
- `en` voor Engels.

`Program.cs` configureert `UseRequestLocalization`, zodat ASP.NET Core bij elke request de juiste culture uit de cookie haalt.

Waarom niet overal hardcoded tekst?

- Hardcoded tekst zoals `Wachtwoorden` of `Save` zou op beide taalinstellingen hetzelfde blijven.
- Door `@T("...")` te gebruiken, verandert de tekst automatisch wanneer de gebruiker de taal wijzigt.
- Productnamen en technische termen zoals `PassManager`, `Vaults`, `Admin`, `Premium`, `URL` en `CSV` blijven bewust hetzelfde of grotendeels hetzelfde, omdat dit termen zijn die in de app als producttaal gebruikt worden.

### Waarom scaffolded Identity pages aanpassen?

Login en register waren eerst gescaffoldde Identity pages. Dat betekent dat Microsoft de basisbestanden in jouw project gezet heeft:

- `Areas/Identity/Pages/Account/Login.cshtml`
- `Areas/Identity/Pages/Account/Login.cshtml.cs`
- `Areas/Identity/Pages/Account/Register.cshtml`
- `Areas/Identity/Pages/Account/Register.cshtml.cs`

Vanaf dat moment zijn het gewone Razor Pages in je eigen project. Je mag dus de layout en zichtbare teksten aanpassen, zolang je de Identity-flow niet breekt.

Wat is aangepast?

- De login/register markup gebruikt nu dezelfde layoutstijl als de rest van de app.
- Zichtbare teksten in `.cshtml` werden vervangen door `@T("...")`.
- De login page gebruikt `Input.Login`, zodat je met gebruikersnaam of e-mailadres kan aanmelden.
- De achterliggende Identity-services zoals `SignInManager` en `UserManager` blijven behouden.

Waarom?

- Anders zouden login/register in een andere taal of stijl blijven dan de rest van de app.
- Door `T("...")` te gebruiken volgen ze dezelfde language switch.
- De scaffolded code blijft de basis voor authenticatie, maar de presentatie past bij jouw project.

Verdedigingszin:

> Ik heb de scaffolded Identity pages niet vervangen door eigen authenticatie. Ik heb vooral de presentatie en teksten aangepast. De login/register flow gebruikt nog altijd ASP.NET Core Identity met `UserManager` en `SignInManager`.

## 14. Home dashboard per rol

De homepagina toont verschillende inhoud afhankelijk van de rol.

Admin:

- Admin dashboard.
- Links naar gebruikersbeheer en auditlogs.

PremiumUser:

- PassManager Dashboard.
- Compacte accountstatus in de hero.
- Security prioriteiten.
- Laatste wachtwoorden.
- Acties naar Vaults, nieuwe Vault en generator.

FreeUser:

- Vaultsoverzicht.
- Compacte limietenstatus in de hero.
- Premium upgradepad voor generator, export, dark mode en onbeperkte opslag.

Bezoeker:

- Publieke startpagina met login/register.
- Korte Premium-promotie zonder dubbele planpanelen.

Belangrijke code:

- `Views/Home/Index.cshtml`

## 15. Mogelijke vragen en korte antwoorden

### Waarom Clean Architecture?

Omdat het de verantwoordelijkheden scheidt: `Domain` kent geen database of web, `Application` bevat businesslogica, `Infrastructure` bevat technische implementaties en `WebUI` bevat controllers/views. Dit maakt onderhoud en testen eenvoudiger.

### Waarom gebruik je ViewModels?

Om enkel de noodzakelijke data naar views te sturen, validatie te plaatsen op formulieren en berekende schermdata te tonen zonder mijn database-entiteiten te vervuilen.

### Waar zit je businesslogica?

Vooral in `VaultService`, `PasswordEntryService`, `PasswordGeneratorService`, `PasswordStrengthService`, `PasswordEncryptionService` en `PasswordHashService`.

### Hoe voorkom je dat gebruikers elkaars Vaults zien?

Bij het ophalen van Vaults en wachtwoorden filter ik altijd op de ingelogde `userId`. Bijvoorbeeld `GetVaultForUser(id, userId)` en `GetEntryForUser(id, userId)`.

### Hoe werkt de exportbeveiliging?

Export is alleen beschikbaar voor `PremiumUser`. De gebruiker moet eerst opnieuw het accountwachtwoord ingeven. In `PasswordEntryController.Export` wordt dit gecontroleerd met `_userManager.CheckPasswordAsync`.

### Hoe werkt wachtwoordgeschiedenis?

Bij een update bewaart `PasswordEntryService.UpdateEntry` voor PremiumUser eerst het oude encrypted password en de oude hash in `PasswordHistory`, daarna wordt de entry aangepast.

### Hoe detecteer je hergebruikte wachtwoorden?

Bij het opslaan wordt een HMAC-SHA256 fingerprint van het wachtwoord bewaard in `PasswordHash`. Voor PremiumUser worden entries gegroepeerd op die fingerprint. Als dezelfde fingerprint bij meerdere entries voorkomt, wordt dit als hergebruik gemarkeerd.

### Waarom encryptie en hashing allebei?

Encryptie is nodig om het wachtwoord later opnieuw te kunnen tonen of exporteren. Hashing/HMAC is nuttig om wachtwoorden te vergelijken voor hergebruik zonder plain text te vergelijken. AES-GCM beschermt de opgeslagen wachtwoorden, HMAC-SHA256 ondersteunt duplicate detection.

### Waarom gebruik je AES-GCM?

AES-GCM is authenticated encryption. Het versleutelt de data en voegt een authentication tag toe. Als ciphertext of tag aangepast wordt, faalt decryptie. Dat is beter dan de vorige AES-CBC-aanpak zonder aparte integriteitscontrole.

### Waar gebruik je Dependency Injection?

In `Program.cs` registreer ik services. Controllers krijgen die services via constructor injection, bijvoorbeeld `VaultController(IVaultService vaultService, UserManager<ApplicationUser> userManager)`.

### Wat is het verschil tussen authenticatie en autorisatie in jouw app?

Authenticatie is inloggen via Identity. Autorisatie bepaalt toegang per rol, bijvoorbeeld alleen PremiumUser voor de generator en alleen Admin voor adminpagina's.

### Waar zie je CSRF-bescherming?

POST-acties zoals create, edit, delete, export en role changes hebben `[ValidateAntiForgeryToken]`.

### Welke database gebruikt de app?

De app gebruikt SQL Server LocalDB met database `BAD_PROJECT_PWMANAGER_DB`, geconfigureerd via `DefaultConnection` in `appsettings.json`.

## 16. Demo-flow voor je verdediging

Een goede demo-volgorde:

1. Toon publieke homepagina.
2. Log in als FreeUser.
3. Toon Vaults en leg de limiet van 3 Vaults uit.
4. Voeg een wachtwoord toe en toon validatie/sterkte.
5. Toon dat FreeUser geen generator of geschiedenis heeft.
6. Log in als PremiumUser of laat Admin een user Premium maken.
7. Toon generator.
8. Wijzig een wachtwoord en toon geschiedenis.
9. Toon hergebruik/oud-wachtwoord security checks.
10. Toon export met wachtwoordcontrole.
11. Log in als Admin.
12. Toon gebruikersbeheer, rolwijziging en auditlogs.
13. Sluit af met de architectuur: Domain, Application, Infrastructure, WebUI.

## 17. Aandachtspunten die je eerlijk kan benoemen

Sterke punten:

- Rollen zijn duidelijk gescheiden.
- Businesslogica zit niet rechtstreeks in views.
- Eigen data wordt gefilterd op gebruiker.
- Password manager domein bevat realistische securityfuncties.
- Project voldoet aan de vereisten.

Verbeterpunten voor productie:

- Encryptie- en HMAC-keys beheren via user secrets, environment variables of een vault-oplossing zoals Azure Key Vault.
- Een migratie voorzien om oude legacy AES-CBC records actief opnieuw te versleutelen naar AES-GCM.
- Meer automatische tests toevoegen.
- Audit logging uitbreiden met IP-adres of type actie.
- Localisatie centraliseren in een volwassen resource-systeem als dit verder groeit.

## 18. Belangrijkste bestanden om te kennen

Architectuur en configuratie:

- `BAD_PROJECT_PWMANAGER/Program.cs`
- `BAD_PROJECT_PWMANAGER/appsettings.json`
- `Infrastructure/Data/ApplicationDbContext.cs`

Domain:

- `Domain/Entities/ApplicationUser.cs`
- `Domain/Entities/Vault.cs`
- `Domain/Entities/PasswordEntry.cs`
- `Domain/Entities/PasswordHistory.cs`
- `Domain/Entities/AuditLog.cs`

Application:

- `Application/Services/VaultService.cs`
- `Application/Services/PasswordEntryService.cs`
- `Application/Interfaces/IUnitOfWork.cs`
- `Application/Interfaces/IRepository.cs`

Infrastructure:

- `Infrastructure/Repositories/Repository.cs`
- `Infrastructure/Repositories/UnitOfWork.cs`

Presentation:

- `BAD_PROJECT_PWMANAGER/Controllers/VaultController.cs`
- `BAD_PROJECT_PWMANAGER/Controllers/PasswordEntryController.cs`
- `BAD_PROJECT_PWMANAGER/Controllers/PasswordGeneratorContrioller.cs`
- `BAD_PROJECT_PWMANAGER/Controllers/AdminController.cs`
- `BAD_PROJECT_PWMANAGER/Views/Home/Index.cshtml`
- `BAD_PROJECT_PWMANAGER/Views/Shared/_Layout.cshtml`
- `BAD_PROJECT_PWMANAGER/ViewModels`

Security en generator services:

- `Application/Services/Security/PasswordEncryptionService.cs`
- `Application/Services/Security/PasswordHashService.cs`
- `Application/Services/Security/SecurityKeyProvider.cs`
- `Application/Services/Security/PasswordStrengthService.cs`
- `Application/Services/Generator/PasswordManagerService.cs`
