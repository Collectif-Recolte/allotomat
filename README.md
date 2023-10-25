# FCP

[Version en français](#PCA)

PCA is a web platform for managing food stamp programs that use rechargeable gift cards. Initially developed for the specific needs of the Carte Proximité program.

## Configuration

### Required software

- The [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet).
- [Node](https://nodejs.org/en/) at the version `14.17.5`
- [Visual Studio](https://visualstudio.microsoft.com/fr/) or [Rider](https://www.jetbrains.com/rider/) for backend development
- [SQL Server Express LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15) – Usually this software is installed together with Visual Studio.
- IDE for frontend development

### Recommended software

- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) to manage your local database.
- [Volta](https://volta.sh/) (automatic Node.js version manager)
- [Visual Studio Code](https://code.visualstudio.com/) for frontend development

### Installation

- Clone the source code on your workstation
- Open a terminal in the `Sig.App.Frontend` folder
  - Run the `npm install` command
  - Confirm frontend build working with `npm run build`
- Open a terminal in the project root folder
  - Run the `dotnet restore` command
  - Confirm the backend build is working with `dotnet build`, and the tests pass with `dotnet test`

### Access the app

Once the backend and frontend are running, you can access the app at: [http://localhost:61759](http://localhost:61759).

### Test Accounts

You can use one of the following test accounts to log in.

By default, 8 accounts are created when the backend is started in development mode. The password is the same for all users: `Abcd1234!!`

#### PCA administrator accounts:

- admin1@example.com
- admin2@example.com

#### Program managers accounts:

- project1@example.com
- project2@example.com

#### Organization managers accounts:

- organization1@example.com
- organization2@example.com

#### Merchants accounts:

- merchant1@example.com
- merchant2@example.com

---

# PCA

PCA est une plateforme électronique pour effectuer la gestion des programmes de coupons alimentaires basés sur des cartes cadeau rechargeables. Initialement conçue pour répondre aux besoins spécifiques du programme Carte Proximité.

### Logiciels requis

- Le [SDK .NET](https://dotnet.microsoft.com/en-us/download/dotnet).
- [Node](https://nodejs.org/en/) à la version `14.17.5`
- [Visual Studio](https://visualstudio.microsoft.com/fr/) ou [Rider](https://www.jetbrains.com/rider/) pour le développement backend
- [SQL Server Express LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15) – Généralement ce logiciel est installé en même temps que Visual Studio.
- Un IDE pour le développement frontend

### Logiciels recommandé

- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) pour gérer votre base de données locale.
- [Volta](https://volta.sh/) (gestionnaire automatique des versions de Node.js)
- [Visual Studio Code](https://code.visualstudio.com/) pour le développement frontend

### Installation

- Clonez le code source sur votre poste
- Ouvrez un terminal dans le dossier `Sig.App.Frontend`
  - Exécutez la commande `npm install`
  - Confirmez le fonctionnement du build frontend avec `npm run build`
- Ouvrez un terminal dans le dossier racine du projet
  - Exécutez la commande `dotnet restore`
  - Confirmez le fonctionnement du build backend avec `dotnet build`, et que les tests passent avec `dotnet test`

### Accéder à l'application

Une fois que le backend et le frontend s'exécutent, vous pouvez accéder à l'application à l'adresse: [http://localhost:61759](http://localhost:61759).

### Comptes de test

Vous pouvez utiliser un des comptes de test suivant pour vous connecter.

Par défaut, 8 comptes sont créés au démarrage du backend en mode développement. Le mot de passe est le même pour tous les utilisateurs: `Abcd1234!!`

#### Comptes administrateur PCA:

- admin1@example.com
- admin2@example.com

#### Comptes des gestionnaires de programme:

- project1@example.com
- project2@example.com

#### Comptes des responsables d'organisation:

- organization1@example.com
- organization2@example.com

#### Comptes marchands:

- merchant1@example.com
- merchant2@example.com
