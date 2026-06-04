using TesteVeste.Application.DTOs;
using TesteVeste.Application.Interfaces;
using TesteVeste.Application.Notifications;
using TesteVeste.Domain.Entities;
using TesteVeste.Domain.Interfaces;
using TesteVeste.Domain.Shared;

namespace TesteVeste.Application.Services;

// =============================================================================
//  TODO — SUA TAREFA
// =============================================================================
//  Implemente todos os métodos desta classe seguindo as regras de negócio abaixo.
//  Consulte o CategoriaService.cs como referência de implementação.
//
//  REGRAS DE NEGÓCIO:
//  [1] Nome é obrigatório e deve ter no máximo 100 caracteres.
//  [2] Preço deve ser maior que zero.
//  [3] Não pode existir dois produtos com o mesmo nome (ignorar capitalização).
//  [4] Um produto INATIVO não pode ser editado (UpdateAsync deve falhar).
//  [5] O DeleteAsync é um "soft delete": apenas define Ativo = false.
//
//  DICA: Use _notifications.AddNotification("mensagem") para registrar erros
//  e retorne CommandResult<T>.Failure(_notifications.Notifications) em caso de falha.
// =============================================================================

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;
    private readonly INotificationService _notifications;

    public ProdutoService(IProdutoRepository repository, INotificationService notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }

    public async Task<CommandResult<PagedResult<ProdutoDto>>> GetAllAsync(int pagina, int tamanhoPagina)
    {
        // TODO: Busque os produtos paginados no repositório e mapeie para ProdutoDto.
        var produtos = await _repository.GetAllAsync(pagina, tamanhoPagina);

        var dto = new PagedResult<ProdutoDto>
        {
            Pagina = produtos.Pagina,
            TamanhoPagina = produtos.TamanhoPagina,
            TotalItens = produtos.TotalItens,
            Itens = produtos.Itens.Select(p => new ProdutoDto
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Preco = p.Preco,
                Estoque = p.Estoque,
                Ativo = p.Ativo,
                DataCadastro = p.DataCadastro,
                CategoriaId = p.CategoriaId,
                CategoriaNome = p.Categoria?.Nome

            })
        };

        return CommandResult<PagedResult<ProdutoDto>>.Success(dto);
    }

    public async Task<CommandResult<ProdutoDto>> GetByIdAsync(int id)
    {
        // TODO: Busque o produto pelo Id.
        //       Se não existir, adicione uma notificação e retorne Failure.
        var produto = await _repository.GetByIdAsync(id);

        if (produto is null)
        {
            _notifications.AddNotification("Produto não encontrado.");

            return CommandResult<ProdutoDto>.Failure(_notifications.Notifications);
        }

        var dto = new ProdutoDto
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            Estoque = produto.Estoque,
            Ativo = produto.Ativo,
            DataCadastro = produto.DataCadastro,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = produto.Categoria?.Nome
        };

        return CommandResult<ProdutoDto>.Success(dto);
    }

    public async Task<CommandResult<ProdutoDto>> CreateAsync(CreateProdutoDto dto)
    {
        // TODO: Valide os campos (regras 1 e 2), verifique duplicidade de nome (regra 3)
        //       e persista o novo produto.
        _notifications.Clear();

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            _notifications.AddNotification("Nome é obrigatório.");
        
        }

        if (!string.IsNullOrWhiteSpace(dto.Nome) && dto.Nome.Length > 100)
        {
            _notifications.AddNotification("Nome deve ter no máximo 100 caracteres.");
        }

        if (dto.Preco <= 0)
        {
            _notifications.AddNotification("Preço deve ser maior que zero.");
        }

        var nomeProdutoExiste = await _repository.ExistsWithNameAsync(dto.Nome);

        if (nomeProdutoExiste) 
        { 
            _notifications.AddNotification("Já existe um produto com esse nome.");
        }

        if (_notifications.HasNotifications)
        {
            return CommandResult<ProdutoDto>.Failure(_notifications.Notifications);
        
        }

        var produto = new Produto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            Estoque = dto.Estoque,
            CategoriaId = dto.CategoriaId,
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };

        await _repository.AddAsync(produto);
        await _repository.SaveChangesAsync();

        var dtoResult = new ProdutoDto
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            Estoque = produto.Estoque,
            Ativo = produto.Ativo,
            DataCadastro = produto.DataCadastro,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = produto.Categoria?.Nome
        };

        return CommandResult<ProdutoDto>.Success(dtoResult);

    }

    public async Task<CommandResult<ProdutoDto>> UpdateAsync(int id, UpdateProdutoDto dto)
    {
        // TODO: Busque o produto, valide se está ativo (regra 4),
        //       valide os campos (regras 1 e 2), verifique duplicidade (regra 3)
        //       e salve as alterações.
        _notifications.Clear();

        var produto = await _repository.GetByIdAsync(id);

        if (produto is null)
        {
            _notifications.AddNotification("Produto não encontrado.");
            return CommandResult<ProdutoDto>.Failure(_notifications.Notifications);
        }

        if (!produto.Ativo)
        {
            _notifications.AddNotification("Produto inativo não pode ser editado.");
            return CommandResult<ProdutoDto>.Failure(_notifications.Notifications);
        }

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            _notifications.AddNotification("Nome é obrigatório.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Nome) && dto.Nome.Length > 100)
        {
            _notifications.AddNotification("Nome deve ter no máximo 100 caracteres.");
        }

        if (dto.Preco <= 0)
        {
            _notifications.AddNotification("Preço deve ser maior que zero.");
        }

        var nomeProdutoExiste = await _repository.ExistsWithNameAsync(dto.Nome, id);

        if (nomeProdutoExiste)
        {
            _notifications.AddNotification("Já existe um produto com esse nome.");
        }

        if (_notifications.HasNotifications)
        {
            return CommandResult<ProdutoDto>.Failure(_notifications.Notifications);
        }

        produto.Nome = dto.Nome;
        produto.Descricao = dto.Descricao;
        produto.Preco = dto.Preco;
        produto.Estoque = dto.Estoque;
        produto.CategoriaId = dto.CategoriaId;

        _repository.Update(produto);
        await _repository.SaveChangesAsync();

        var result = new ProdutoDto
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            Estoque = produto.Estoque,
            Ativo = produto.Ativo,
            DataCadastro = produto.DataCadastro,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = produto.Categoria?.Nome
        };

        return CommandResult<ProdutoDto>.Success(result);
    }

    public async Task<CommandResult<bool>> DeleteAsync(int id)
    {
        // TODO: Busque o produto. Se não existir, retorne Failure.
        //       Caso contrário, defina Ativo = false e salve (regra 5).
        _notifications.Clear();

        var produto = await _repository.GetByIdAsync(id);

        if (produto is null)
        {
            _notifications.AddNotification("Produto não encontrado.");
            return CommandResult<bool>.Failure(_notifications.Notifications);
        }

        produto.Ativo = false;

        _repository.Update(produto);
        await _repository.SaveChangesAsync();

        return CommandResult<bool>.Success(true);
    }
}
