﻿using SharpBlogX.Dto.Blog;
using SharpBlogX.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpBlogX.Caching.Blog
{
    public partial interface IBlogCacheService
    {
        /// <summary>
        /// Get the list of friendlinks from the cache.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<BlogResponse<List<FriendLinkDto>>> GetFriendLinksAsync(Func<Task<BlogResponse<List<FriendLinkDto>>>> func);
    }
}