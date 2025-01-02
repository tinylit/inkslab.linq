﻿using System;
using System.Collections.Generic;
using Inkslab.Linq.Abilities;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库路由器。
    /// </summary>
    public interface IRepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        IInsertable<TEntity> AsInsertable(List<TEntity> entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IUpdateable<TEntity> AsUpdateable(List<TEntity> entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IDeleteable<TEntity> AsDeleteable(List<TEntity> entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <param name="entries">项目集合。</param>
        IInsertable<TEntity> AsInsertable(string shardingKey, List<TEntity> entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IUpdateable<TEntity> AsUpdateable(string shardingKey, List<TEntity> entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IDeleteable<TEntity> AsDeleteable(string shardingKey, List<TEntity> entries);
    }
}
