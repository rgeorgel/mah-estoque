# Plano Inicial - Sistema de Controle de Estoque

## Visão Geral

Aplicação web para controle de estoque e vendas com múltiplos inquilinos (tenants). Tecnologia: .NET 10 (API) + HTML/JS/CSS (Frontend sem frameworks) + PostgreSQL + Docker Compose.

---

## Estrutura do Projeto

```
mah-estoque/
├── src/
│   ├── api/
│   │   └── MahEstoque.Api/
│   │       ├── Controllers/
│   │       ├── Services/
│   │       ├── Data/
│   │       ├── Models/
│   │       ├── DTOs/
│   │       ├── Middleware/
│   │       └── Program.cs
│   └── frontend/
│       ├── index.html
│       ├── login.html
│       ├── register.html
│       ├── dashboard.html
│       ├── produtos.html
│       ├── vendas.html
│       ├── relatorios.html
│       ├── usuarios.html
│       └── css/
│           └── style.css
├── docs/
│   └── 001_initial-plan.md
├── docker-compose.yml
└── README.md
```

---

## Banco de Dados (PostgreSQL)

### Esquema

```sql
-- Tenants (empresas/organizações)
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Usuários (pertencem a um tenant)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    role VARCHAR(20) NOT NULL DEFAULT 'employee',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Produtos (escopo por tenant)
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    sku VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    acquired_value DECIMAL(10,2) NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    min_stock INT DEFAULT 5,
    category VARCHAR(100),          -- opcional
    supplier VARCHAR(100),           -- opcional
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Transações (escopo por tenant)
CREATE TABLE transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    product_id UUID REFERENCES products(id),
    type VARCHAR(20) NOT NULL, -- 'sale', 'purchase', 'adjustment'
    quantity INT NOT NULL,
    unit_value DECIMAL(10,2) NOT NULL,
    total_value DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices
CREATE INDEX idx_tenant_users ON users(tenant_id);
CREATE INDEX idx_tenant_products ON products(tenant_id);
CREATE INDEX idx_tenant_transactions ON transactions(tenant_id);
CREATE INDEX idx_product_transactions ON transactions(product_id);
```

---

## Autenticação e Autorização

### JWT
- Token com expiração de 24 horas
- Password hashing com bcrypt
- Middleware de autenticação

### Roles
- **admin**: Acesso total, gerencia usuários
- **manager**: Produtos + transações + relatórios
- **employee**: Apenas registra vendas

### Endpoints de Autenticação
| Método | Endpoint | Descrição |
|--------|----------|------------|
| POST | /api/auth/register | Registrar tenant + primeiro usuário admin |
| POST | /api/auth/login | Login, retorna JWT |
| GET | /api/auth/me | Obter usuário atual |

---

## API Endpoints

### Usuários
| Método | Endpoint | Role | Descrição |
|--------|----------|------|------------|
| GET | /api/users | admin | Listar usuários |
| POST | /api/users | admin | Criar usuário |
| PUT | /api/users/{id} | admin | Atualizar usuário |
| DELETE | /api/users/{id} | admin | Excluir usuário |

### Produtos
| Método | Endpoint | Role | Descrição |
|--------|----------|------|------------|
| GET | /api/products | manager+ | Listar produtos |
| POST | /api/products | manager+ | Criar produto |
| GET | /api/products/{id} | manager+ | Obter produto |
| PUT | /api/products/{id} | manager+ | Atualizar produto |
| DELETE | /api/products/{id} | manager+ | Excluir produto |
| GET | /api/products/low-stock | manager+ | Produtos com estoque baixo |

### Transações
| Método | Endpoint | Role | Descrição |
|--------|----------|------|------------|
| GET | /api/transactions | manager+ | Listar transações |
| POST | /api/transactions | employee+ | Registrar venda/compra |

### Relatórios
| Método | Endpoint | Role | Descrição |
|--------|----------|------|------------|
| GET | /api/reports/sales | manager+ | Relatório de vendas |
| GET | /api/reports/profit | manager+ | Relatório de lucro |
| GET | /api/reports/stock | manager+ | Relatório de estoque |
| GET | /api/reports/stock-by-category | manager+ | Estoque por categoria |

### Dashboard
| Método | Endpoint | Role | Descrição |
|--------|----------|------|------------|
| GET | /api/dashboard/stats | manager+ | Métricas do dashboard |

---

## Frontend (pt-BR)

### Páginas

| Página | Descrição |
|--------|------------|
| /index.html | Redireciona para login ou dashboard |
| /login.html | Formulário de login |
| /register.html | Registro de tenant + primeiro usuário |
| /dashboard.html | Métricas principais |
| /produtos.html | CRUD de produtos |
| /vendas.html | Registro de vendas |
| /relatorios.html | Relatórios com gráficos |
| /usuarios.html | Gerenciamento de usuários (admin) |

### Dashboard - Métricas (pt-BR)
- Total de produtos em estoque
- Valor total em estoque
- Vendas hoje (quantidade e valor)
- Receita do período
- Lucro bruto
- Produtos com estoque baixo
- Produtos por categoria (gráfico)
- Vendas por dia (gráfico)

### Produtos - Colunas
- SKU
- Nome
- Categoria (opcional)
- Fornecedor (opcional)
- Valor de aquisição
- Quantidade
- Ações

### Relatórios (Chart.js)
- Vendas por período → Gráfico de barras
- Estoque por categoria → Gráfico de pizza
- Evolução de vendas → Gráfico de linha
- Margem de lucro por produto

---

## Docker Compose

```yaml
version: '3.8'

services:
  api:
    build: ./src/api
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=mahestoque;Username=postgres;Password=${DB_PASSWORD}
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__ExpirationHours=24
    depends_on:
      - db

  db:
    image: postgres:16
    environment:
      - POSTGRES_DB=mahestoque
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  nginx:
    image: nginx:alpine
    volumes:
      - ./src/frontend:/usr/share/nginx/html:ro
    ports:
      - "80:80"
    depends_on:
      - api

volumes:
  pgdata:
```

---

## Requisitos de Segurança (Produção)

- HTTPS
- Password hashing (bcrypt)
- JWT com expiração
- CORS configurado
- Input validation (DataAnnotations)
- Rate limiting básico
- Isolamento por tenant (row-level)

---

## Fases de Implementação

1. **Fase 1 - Setup**: Inicialização do projeto, Docker, EF migrations
2. **Fase 2 - Autenticação**: JWT, registro de tenant, login, isolamento de tenant
3. **Fase 3 - Produtos**: CRUD com categoria e fornecedor
4. **Fase 4 - Transações**: Vendas com decremento de estoque
5. **Fase 5 - Dashboard**: Estatísticas e alertas de estoque baixo
6. **Fase 6 - Relatórios**: Vendas, lucro, estoque (com gráficos)
7. **Fase 7 - Usuários**: Gerenciamento de usuários admin
8. **Fase 8 - Polish**: CSS responsivo, pt-BR, testes