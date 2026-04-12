# Plano: Catalogo Publico, Imagens e Integracao WhatsApp

Data: 2026-04-12

## Objetivo

1. Adicionar imagens aos produtos
2. Gerar catalogo publico para clientes com carrinho e finalizacao via WhatsApp
3. Preparar sistema para vendas online futuras
4. Admin controla visibilidade, preco de venda e desconto por produto

---

## Fase 1 — Imagens nos Produtos

### Banco de Dados
- Nova tabela `ProductImage`: `Id`, `ProductId` (FK), `TenantId`, `FileName`, `StoredPath`, `IsPrimary` (bool), `DisplayOrder`, `CreatedAt`
- Suporte a multiplas imagens por produto com uma marcada como principal

### Backend
- Upload via `POST /api/products/{id}/images` (multipart/form-data)
- Delete via `DELETE /api/products/{id}/images/{imageId}`
- Arquivos salvos em `/uploads/{tenantId}/{productId}/` dentro do container
- Nginx configurado para servir `/uploads/` como arquivos estaticos
- Tamanho maximo e tipos aceitos validados no controller (JPEG, PNG, WebP, max 5MB)
- DTOs de produto passam a incluir `Images[]` com URL publica

### Frontend Admin (produtos.html)
- Campo de upload de imagem no formulario de produto (arrastar/soltar ou clicar)
- Preview das imagens com botao de remover
- Marcar imagem como principal (exibida no catalogo)

---

## Fase 2 — Campos de Venda no Produto

### Banco de Dados
Novas colunas na tabela `Product`:
- `SalePrice` (decimal, nullable) — preco de venda
- `SalePriceDiscount` (decimal, nullable) — preco com desconto
- `IsVisible` (bool, default: false) — visivel no catalogo
- `Description` (text, nullable) — descricao para o catalogo

### Backend
- DTOs atualizados para incluir os novos campos
- `PUT /api/products/{id}` aceita os novos campos
- Novo endpoint publico (sem autenticacao): `GET /api/catalog/{tenantSlug}/products` — retorna apenas produtos com `IsVisible = true`, com dados limitados (sem custo de aquisicao)

### Frontend Admin (produtos.html)
- Novos campos no formulario: Preco de Venda, Preco com Desconto, Descricao, toggle Visivel no Catalogo
- Indicador visual na lista de produtos mostrando se esta visivel ou nao

---

## Fase 3 — Catalogo Publico

### Nova Pagina: catalogo.html
- Rota publica (sem login): `/catalogo.html?t={slug}`
- Layout de vitrine: grid de cards de produtos com imagem, nome, descricao curta, preco original riscado + preco com desconto

### Carrinho de Compras (localStorage)
- Carrinho persistido em `localStorage` com chave `cart_{tenantSlug}`
- Icone de carrinho flutuante com badge de quantidade
- Botao "Adicionar ao Carrinho" em cada card
- Painel lateral deslizante (drawer) com itens do carrinho, quantidades editaveis, subtotal

### Finalizacao via WhatsApp
- Botao "Finalizar Pedido" no carrinho
- Mensagem formatada gerada automaticamente:
  ```
  Ola! Estou interessado em comprar os seguintes produtos:
  
  - 2x Produto A (R$ 45,00)
  - 1x Produto B - Cor: Azul, Tam: M (R$ 89,90)
  
  Total: R$ 179,90
  ```
- Abre `https://wa.me/{numero}?text={mensagem_encodada}`
- Numero do WhatsApp configurado por tenant

### Tenant Config (WhatsApp)
- Nova coluna `WhatsappNumber` e `Slug` na tabela `Tenant`
- Endpoint admin: `PUT /api/tenant/config`
- Pagina de configuracoes no admin para o numero do WhatsApp e slug da loja

---

## Fase 4 — Preparacao para Vendas Online Futuras

### Estrutura de Pedidos (schema sem fluxo completo)
- Tabela `Order`: `Id`, `TenantId`, `Status` (enum: Pending/Confirmed/Cancelled), `CustomerName`, `CustomerPhone`, `TotalValue`, `CreatedAt`
- Tabela `OrderItem`: `Id`, `OrderId`, `ProductId`, `VariantId`, `Quantity`, `UnitPrice`
- Ainda nao usado pelo fluxo atual (WhatsApp), mas pronto para integracao de pagamento futura

### API de Catalogo com Slug de Tenant
- Campo `Slug` (unico) na tabela `Tenant`
- URL do catalogo: `/catalogo.html?t=mah-loja`

---

## Ordem de Implementacao

| Ordem | Fase | Dependencias |
|-------|------|--------------|
| 1 | Fase 2 (campos de venda + visibilidade) | Nenhuma |
| 2 | Fase 1 (imagens) | Upload de arquivo, Nginx |
| 3 | Fase 3 (catalogo + carrinho + WhatsApp) | Fases 1 e 2 |
| 4 | Fase 4 (estrutura de pedidos) | Fase 3 |

---

## Impacto em Arquivos Existentes

| Arquivo | Mudanca |
|---------|---------|
| `Product.cs` (model) | +4 colunas novas |
| `Program.cs` | Migracao SQL das novas tabelas/colunas + rota de uploads |
| `ProductService.cs` | Logica de imagens, filtro `IsVisible` |
| `produtos.html` | Novos campos no form + upload de imagem |
| `nginx.conf` | Servir `/uploads/` estaticamente |
| `docker-compose.yml` | Volume para `/uploads/` persistido |

## Novos Arquivos

| Arquivo | Descricao |
|---------|-----------|
| `catalogo.html` | Pagina publica do catalogo |
| `js/catalogo.js` | Logica do catalogo, carrinho, WhatsApp |
| `ProductImage.cs` | Model de imagem |
| `TenantConfig` | Coluna WhatsappNumber + Slug no Tenant |
| `Order.cs` / `OrderItem.cs` | Models de pedido (estrutura futura) |
| `CatalogController.cs` | Endpoints publicos do catalogo |
| `configuracoes.html` | Pagina admin de configuracoes do tenant |
