# Functionaliteiten per rol - PassManager

Dit document beschrijft de zichtbare en technische functionaliteiten van PassManager per gebruikersrol. De rollen worden beheerd via ASP.NET Core Identity en worden aangemaakt bij het starten van de applicatie in `BAD_PROJECT_PWMANAGER/Program.cs`.

## Rollen

De applicatie gebruikt drie rollen:

- `Admin`
- `FreeUser`
- `PremiumUser`

Nieuwe gebruikers krijgen standaard de rol `FreeUser` via `Areas/Identity/Pages/Account/Register.cshtml.cs`.

## Niet-aangemelde bezoeker

Een bezoeker zonder login kan:

- De publieke startpagina bekijken.
- Een account registreren.
- Inloggen.
- Wisselen tussen Nederlands en Engels via de taalkeuze.

Een bezoeker zonder login kan niet:

- Vaults bekijken of beheren.
- Wachtwoorden bekijken, aanmaken, wijzigen, verwijderen of exporteren.
- De password generator gebruiken.
- Wisselen naar dark mode.
- Adminpagina's openen.

Technisch:

- Beveiligde controllers gebruiken `[Authorize]`.
- De generator gebruikt `[Authorize(Roles = "PremiumUser")]`.
- CSV-export gebruikt `[Authorize(Roles = "PremiumUser")]`.
- Dark mode wordt in de layout alleen getoond voor `PremiumUser` en `Admin`.
- Adminpagina's gebruiken `[Authorize(Roles = "Admin")]`.

UX:

- De publieke startpagina focust op registreren/inloggen en toont kort waarom Premium waarde heeft.
- De publieke startpagina herhaalt planinformatie niet in meerdere panelen.

## FreeUser

Een `FreeUser` is een gewone gebruiker met beperkte limieten.

### Account

Een FreeUser kan:

- Inloggen en uitloggen.
- Profielgegevens beheren:
  - gebruikersnaam
  - voornaam
  - achternaam
  - telefoonnummer
  - profielfoto
- Profielfoto uploaden met validatie op bestandstype en grootte.
- Taal wijzigen.

Belangrijke code:

- `Areas/Identity/Pages/Account/Register.cshtml.cs`
- `Areas/Identity/Pages/Account/Login.cshtml.cs`
- `Areas/Identity/Pages/Account/Manage/Index.cshtml.cs`
- `Controllers/ProfileImageController.cs`
- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/site.js`

### Vaults

Een FreeUser kan:

- Eigen Vaults bekijken.
- Een Vault aanmaken.
- Een Vault wijzigen.
- Een Vault verwijderen.
- Vanuit een Vault de bijhorende wachtwoorden openen.

Limiet:

- Maximaal 3 Vaults.

Belangrijke code:

- `Controllers/VaultController.cs`
- `Application/Services/VaultService.cs`
- `ViewModels/Vault/VaultFormViewModel.cs`

De limiet wordt gecontroleerd in:

- `VaultController.Create`
- `VaultService.CreateVault`

### Wachtwoorden

Een FreeUser kan:

- Wachtwoorden binnen eigen Vaults bekijken.
- Een wachtwoord toevoegen.
- Details van een wachtwoord openen.
- Een wachtwoord wijzigen.
- Een wachtwoord verwijderen.
- Een wachtwoord kopieren.
- Wachtwoordsterkte zien: `Zwak`, `Gemiddeld`, `Sterk`.

Limiet:

- Maximaal 10 opgeslagen wachtwoorden over alle eigen Vaults heen.

Belangrijke code:

- `Controllers/PasswordEntryController.cs`
- `Application/Services/PasswordEntryService.cs`
- `Application/Services/Security/PasswordEncryptionService.cs`
- `Application/Services/Security/PasswordHashService.cs`
- `Application/Services/Security/PasswordStrengthService.cs`

Beperkingen voor FreeUser:

- Geen password generator.
- Geen wachtwoordgeschiedenis.
- Geen premium security checks voor hergebruikte of oude wachtwoorden.
- Geen CSV-export.
- Geen dark mode.
- Geen onbeperkte Vaults/wachtwoorden.

UX:

- De homepagina toont de belangrijkste accountstatus compact in de hero.
- De exportknop wordt vervangen door een Premium-upgradeknop.

## PremiumUser

Een `PremiumUser` heeft alle FreeUser-functionaliteiten plus extra security- en productiviteitsfuncties.

### Alles van FreeUser

Een PremiumUser kan alles wat een FreeUser kan:

- Account beheren.
- Vaults beheren.
- Wachtwoorden beheren.
- Exporteren na accountwachtwoordcontrole.
- Taal wijzigen.
- Dark mode gebruiken.

### Onbeperkte Vaults en wachtwoorden

Een PremiumUser heeft geen FreeUser-limieten:

- Geen limiet van 3 Vaults.
- Geen limiet van 10 wachtwoorden.

Belangrijke code:

- `VaultService.CreateVault`
- `PasswordEntryService.CreateEntry`
- `VaultController.Create`
- `PasswordEntryController.Create`

### Password generator

Een PremiumUser kan de password generator gebruiken.

Generatoropties:

- Lengte tussen 8 en 128 tekens.
- Hoofdletters.
- Kleine letters.
- Cijfers.
- Speciale tekens.
- Gelijkaardige tekens vermijden, zoals `0`, `O`, `1`, `I`, `l`.
- Verwarrende symbolen vermijden.

Technisch:

- Alleen toegankelijk voor `PremiumUser`.
- Gebruikt `RandomNumberGenerator` voor cryptografisch sterkere randomisatie.
- Garandeert dat elk gekozen tekentype minstens een keer voorkomt.

Belangrijke code:

- `Controllers/PasswordGeneratorContrioller.cs` (bevat de klasse `PasswordGeneratorController`)
- `Application/Services/Generator/PasswordManagerService.cs` (bevat de klasse `PasswordGeneratorService`)
- `ViewModels/Generator/PasswordGeneratorViewModel.cs`

### CSV-export

Een PremiumUser kan een Vault exporteren naar CSV na controle van het accountwachtwoord.

Technisch:

- Alleen toegankelijk voor `PremiumUser`.
- De exportactie is beschermd met `[Authorize(Roles = "PremiumUser")]`.
- De gebruiker moet het accountwachtwoord opnieuw ingeven.
- De export bevat leesbare wachtwoorden en wordt daarom expliciet bevestigd.

Belangrijke code:

- `Controllers/PasswordEntryController.cs` (`Export`)
- `Views/PasswordEntry/Index.cshtml`
- `Application/Services/Security/PasswordEncryptionService.cs`

### Security checks

Een PremiumUser krijgt extra signalen:

- Hergebruikte wachtwoorden detecteren.
- Oude wachtwoorden detecteren, vanaf 90 dagen.
- Security prioriteiten op de homepagina.

Belangrijke code:

- `Controllers/PasswordEntryController.cs` (`Index`, `Details`)
- `Views/Home/Index.cshtml`
- `Application/Services/Security/PasswordStrengthService.cs`

### Dark mode

Een PremiumUser kan wisselen tussen licht en donker thema.

Technisch:

- De layout geeft alleen voor `PremiumUser` en `Admin` de theme toggle weer.
- `wwwroot/js/site.js` forceert light mode wanneer de gebruiker geen premium theme-toegang heeft.
- De keuze wordt bewaard in `localStorage` met sleutel `passmanager-theme`.

Belangrijke code:

- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/site.js`

### Wachtwoordgeschiedenis

Bij het wijzigen van een wachtwoord wordt voor PremiumUser het oude wachtwoord opgeslagen in de geschiedenis.

Een PremiumUser kan:

- Oude wachtwoordversies bekijken op de details-pagina.
- Historiek raadplegen voor een password entry.

Belangrijke code:

- `Application/Services/PasswordEntryService.cs` (`UpdateEntry`, `GetHistoryForEntry`)
- `Controllers/PasswordEntryController.cs` (`Details`, `History`)
- `Domain/Entities/PasswordHistory.cs`

## Admin

Een `Admin` beheert gebruikers en controleert audit logs.

### Admin dashboard

Een Admin ziet een aangepaste startpagina met links naar:

- Gebruikersbeheer.
- Auditlogs.

De admin startpagina is bewust compact en herhaalt de adminacties niet in extra statuspanelen.

Belangrijke code:

- `Views/Home/Index.cshtml`
- `Controllers/AdminController.cs`

### Gebruikers beheren

Een Admin kan:

- Alle gebruikers bekijken.
- Rollen wijzigen naar:
  - `FreeUser`
  - `PremiumUser`
  - `Admin`
- Gebruikers upgraden naar Premium.
- Gebruikers terugzetten naar Free.

Beveiliging:

- Een admin kan zichzelf niet via de role dropdown degraderen naar een niet-admin rol.
- Acties zijn beschermd met `[ValidateAntiForgeryToken]`.

Belangrijke code:

- `AdminController.Users`
- `AdminController.SetRole`
- `AdminController.MakePremium`
- `AdminController.MakeFree`
- `Views/Admin/Users.cshtml`

### Auditlogs bekijken

Een Admin kan auditlogs bekijken van recente acties.

Geloggede acties:

- Vault aangemaakt.
- Vault gewijzigd.
- Vault verwijderd.
- Password entry aangemaakt.
- Password entry gewijzigd.
- Password entry verwijderd.
- Gebruikersrol gewijzigd.
- Gebruiker premium/free gemaakt.

Belangrijke code:

- `AdminController.AuditLogs`
- `Domain/Entities/AuditLog.cs`
- `Application/Services/VaultService.cs`
- `Application/Services/PasswordEntryService.cs`

### Wat Admin niet doet

Een Admin beheert gebruikers en rollen, maar leest niet rechtstreeks wachtwoorden van gebruikers via de adminpagina's. De adminomgeving is gericht op beheer en audit, niet op inhoudelijke toegang tot de Vaults van andere gebruikers.

## Overzichtstabel

| Functionaliteit | Bezoeker | FreeUser | PremiumUser | Admin |
| --- | --- | --- | --- | --- |
| Registreren | Ja | Nee | Nee | Nee |
| Inloggen | Ja | Ja | Ja | Ja |
| Profiel beheren | Nee | Ja | Ja | Ja |
| Taal wijzigen | Ja | Ja | Ja | Ja |
| Dark mode gebruiken | Nee | Nee | Ja | Ja |
| Vaults bekijken | Nee | Eigen Vaults | Eigen Vaults | Nee, adminbeheer |
| Vault aanmaken | Nee | Max. 3 | Onbeperkt | Nee, adminbeheer |
| Wachtwoorden beheren | Nee | Max. 10 | Onbeperkt | Nee, adminbeheer |
| Wachtwoordsterkte zien | Nee | Ja | Ja | Nee |
| Password generator | Nee | Nee | Ja | Nee |
| Wachtwoordgeschiedenis | Nee | Nee | Ja | Nee |
| Hergebruikte wachtwoorden detecteren | Nee | Nee | Ja | Nee |
| Oude wachtwoorden detecteren | Nee | Nee | Ja | Nee |
| CSV export | Nee | Nee | Ja, met wachtwoordcontrole | Nee |
| Gebruikersrollen beheren | Nee | Nee | Nee | Ja |
| Auditlogs bekijken | Nee | Nee | Nee | Ja |

## Entiteiten die de functionaliteiten ondersteunen

De app gebruikt meer dan drie gelinkte entiteiten buiten Identity:

- `Vault`
- `PasswordEntry`
- `PasswordHistory`
- `PasswordExport`
- `AuditLog`

Belangrijke relaties:

- Een `Vault` hoort bij een gebruiker via `UserId`.
- Een `Vault` heeft meerdere `PasswordEntries`.
- Een `PasswordEntry` hoort bij een `Vault`.
- Een `PasswordEntry` heeft meerdere `PasswordHistories`.
- `AuditLog` bewaart acties van gebruikers.
