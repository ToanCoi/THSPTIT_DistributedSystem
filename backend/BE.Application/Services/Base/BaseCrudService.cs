using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces;
using BE.Application.Exceptions;
using BE.Domain.Repos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Services.Base
{
    /// <summary>
    /// Base service với CRUD operations và phân trang
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TDto">DTO type</typeparam>
    /// <typeparam name="TCreateDto">Create DTO type</typeparam>
    /// <typeparam name="TUpdateDto">Update DTO type</typeparam>
    public abstract class BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : class
    {
        protected readonly IBaseRepo _baseRepo;
        protected readonly ILogger _logger;
        protected abstract string TableName { get; }
        protected abstract string[] SelectColumns { get; }
        protected abstract TDto MapToDto(TEntity entity);

        protected BaseCrudService(IBaseRepo baseRepo, ILogger logger)
        {
            _baseRepo = baseRepo;
            _logger = logger;
        }

        /// <summary>
        /// Lấy theo ID
        /// </summary>
        public virtual async Task<TDto> GetByIdAsync(Guid id)
        {
            var entity = await _baseRepo.GetByIdAsync<TEntity>(id);
            if (entity == null)
            {
                throw new BusinessException($"Không tìm thấy {TableName}", 404);
            }
            return MapToDto(entity);
        }

        /// <summary>
        /// Lấy tất cả
        /// </summary>
        public virtual async Task<IEnumerable<TDto>> GetAllAsync()
        {
            var entities = await _baseRepo.GetPaging<TEntity>(
                string.Join(", ", SelectColumns),
                0,
                1000,
                "created_date DESC",
                null
            );
            var result = new List<TDto>();
            foreach (var entity in (System.Collections.IEnumerable)entities.Data)
            {
                result.Add(MapToDto((TEntity)entity));
            }
            return result;
        }

        /// <summary>
        /// Lấy phân trang
        /// </summary>
        public virtual async Task<PagingResult<TDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var sort = $"{filter.sort_field} {filter.sort_order}";
            var pagingResult = await _baseRepo.GetPaging<TEntity>(
                string.Join(", ", SelectColumns),
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<TDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in (System.Collections.IEnumerable)pagingResult.Data)
                {
                    dtos.Add(MapToDto((TEntity)entity));
                }
            }

            return new PagingResult<TDto>
            {
                data = dtos,
                total = pagingResult.Data?.Count ?? 0
            };
        }

        /// <summary>
        /// Tạo mới
        /// </summary>
        public virtual async Task<TDto> CreateAsync(TCreateDto dto)
        {
            var entity = MapToEntity(dto);
            await _baseRepo.InsertAsync<TEntity>(entity);
            _logger.LogInformation("Tạo {TableName} mới", TableName);
            return MapToDto((TEntity)entity);
        }

        /// <summary>
        /// Cập nhật
        /// </summary>
        public virtual async Task<TDto> UpdateAsync(Guid id, TUpdateDto dto)
        {
            var entity = await _baseRepo.GetByIdAsync<TEntity>(id);
            if (entity == null)
            {
                throw new BusinessException($"Không tìm thấy {TableName}", 404);
            }

            UpdateEntity(entity, dto);
            await _baseRepo.UpdateAsync<TEntity>(entity);
            _logger.LogInformation("Cập nhật {TableName} [{Id}]", TableName, id);
            return MapToDto(entity);
        }

        /// <summary>
        /// Xóa
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _baseRepo.GetByIdAsync<TEntity>(id);
            if (entity == null)
            {
                throw new BusinessException($"Không tìm thấy {TableName}", 404);
            }

            var result = await _baseRepo.DeleteAsync(entity);
            _logger.LogInformation("Xóa {TableName} [{Id}]", TableName, id);
            return result;
        }

        /// <summary>
        /// Map DTO sang Entity (override trong class con)
        /// </summary>
        protected abstract TEntity MapToEntity(TCreateDto dto);

        /// <summary>
        /// Update entity từ DTO (override trong class con)
        /// </summary>
        protected abstract void UpdateEntity(TEntity entity, TUpdateDto dto);
    }
}
