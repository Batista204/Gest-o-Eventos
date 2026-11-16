# Sistema Web de Gestão de Eventos Acadêmicos e Emissão de Certificados

Projeto da disciplina **Estágio III** do curso de **Sistemas de Informação**.

- **Professor:** Jhemerson Kistner
- **Aluno(a):** [preencher nome completo]
- **Instituição:** UNIDESC - Centro Universitário de Desenvolvimento do Centro-Oeste

## Sobre o projeto

Aplicação web que centraliza a organização de eventos acadêmicos: cadastro de eventos, inscrições com controle de vagas, registro de presença, emissão de certificados em PDF e validação pública de autenticidade.

### Perfis de usuário

| Perfil | O que faz |
|---|---|
| **Administrador/Organizador** | Cria e gerencia eventos, consulta inscritos, registra presença, libera certificados e consulta relatórios (com exportação CSV) |
| **Participante (estudante)** | Consulta eventos abertos, inscreve-se, cancela inscrição, acompanha presença, baixa certificados e consulta seu histórico |
| **Visitante** | Valida a autenticidade de um certificado pelo código, sem precisar de login |

## Tecnologias

- C# e ASP.NET Core MVC (.NET 10, com a versão do SDK fixada em `global.json`)
- Entity Framework Core e Microsoft SQL Server
- ASP.NET Core Identity (autenticação, perfis e hash de senhas)
- Bootstrap 5, HTML5, CSS3 e JavaScript
- QuestPDF (geração de certificados em PDF)
- xUnit (testes automatizados)

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download), conforme `global.json`
- SQL Server Express (ou outra edição do SQL Server) em execução
- Git

## Como executar

Os comandos abaixo são executados na pasta raiz do repositório (PowerShell).

### 1. Clonar o repositório

```powershell
git clone https://github.com/Batista204/Gest-o-Eventos.git
cd Gest-o-Eventos
```

### 2. Restaurar as ferramentas do projeto

```powershell
dotnet tool restore
```

### 3. Configurar os segredos (User Secrets)

A *connection string* e as credenciais do administrador inicial **não** ficam no repositório. Substitua os valores entre `<...>` pelos seus:

```powershell
dotnet user-secrets init --project src/GestaoEventos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=GestaoEventos;Trusted_Connection=True;TrustServerCertificate=True;" --project src/GestaoEventos
dotnet user-secrets set "SeedAdmin:Email" "<e-mail-do-administrador>" --project src/GestaoEventos
dotnet user-secrets set "SeedAdmin:Senha" "<senha-forte>" --project src/GestaoEventos
dotnet user-secrets set "SeedAdmin:Nome" "Administrador" --project src/GestaoEventos
```

A senha deve ter no mínimo 8 caracteres, com letra maiúscula, minúscula e número. Se a instância do SQL Server tiver outro nome, ajuste o valor de `Server`.

> `TrustServerCertificate=True` é aceitável apenas em ambiente de desenvolvimento local.

### 4. Criar o banco de dados

```powershell
dotnet ef database update --project src/GestaoEventos
```

### 5. Executar a aplicação

```powershell
dotnet run --project src/GestaoEventos
```

Abra o endereço exibido no terminal (por padrão `http://localhost:5012`). Na primeira execução, o sistema cria os perfis `Admin` e `Participante` e o administrador inicial com o e-mail e a senha configurados no passo 3.

## Configuração da instituição

O nome da instituição e os dados de assinatura do certificado ficam no `src/GestaoEventos/appsettings.json`, na seção `Instituicao`:

```json
"Instituicao": {
  "Nome": "UNIDESC - Centro Universitário de Desenvolvimento do Centro-Oeste",
  "SignatarioNome": "",
  "SignatarioCargo": ""
}
```

Para incluir o logotipo no certificado, salve a imagem em `src/GestaoEventos/Assets/logo-instituicao.png`. Sem o arquivo, o certificado é gerado apenas com o nome da instituição.

## Testes automatizados

Os testes usam um banco SQL Server temporário, criado e removido a cada teste. O banco `GestaoEventos` **não** é alterado.

```powershell
dotnet test tests/GestaoEventos.Tests
```

Por padrão, os testes usam `localhost\SQLEXPRESS`. Para outro servidor, defina a variável de ambiente `TESTES_SQLSERVER` antes de executar.

## Estrutura do repositório

```
├── docs/                          Documentação do projeto
├── src/GestaoEventos/
│   ├── Areas/Admin/               Área do administrador (eventos, presença, certificados, relatórios)
│   ├── Areas/Participante/        Área do participante (eventos, inscrições, histórico, certificados)
│   ├── Controllers/               Home, Account e validação pública de certificados
│   ├── Data/                      DbContext, configurações do EF Core, migrations e seed
│   ├── Models/                    Entidades, enums, constantes e ViewModels
│   ├── Services/                  Regras de negócio (interfaces e implementações)
│   ├── Views/                     Layout e telas compartilhadas
│   └── Assets/                    Arquivos usados na geração do certificado
├── tests/GestaoEventos.Tests/     Testes automatizados (xUnit)
├── global.json                    Versão do SDK .NET
└── GestaoEventos.slnx             Solução
```

A aplicação segue arquitetura em camadas: **Controllers** (apresentação), **Services** (regras de negócio) e **Data** (acesso a dados com Entity Framework Core).

## Segurança

- Senhas armazenadas com hash (ASP.NET Core Identity), nunca em texto puro.
- Bloqueio temporário da conta após 5 tentativas de login inválidas.
- Autorização por perfil em todas as áreas e proteção anti-CSRF em todos os formulários de escrita (verificadas por testes automatizados).
- Credenciais e *connection string* fora do repositório (User Secrets).
- Página pública de validação com limite de consultas por IP e exibição mínima de dados.
- Exportação CSV com neutralização de fórmulas.

## Licenças de terceiros

A geração de PDF utiliza a biblioteca **QuestPDF** sob a Community License versão 3.0 (questpdf.com/license). O uso neste projeto é acadêmico e sem fins lucrativos. O uso comercial por organizações acima do limite de receita previsto na licença exige licença paga.

## Documentação

Os documentos do projeto (requisitos, regras de negócio, modelagem, arquitetura, testes e manual) estão na pasta `docs/`.
