﻿using SharpBlogX.Dto.Blog;
using SharpBlogX.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpBlogX.Caching.Blog.Impl
{
    public partial class BlogCacheService
    {
        public async Task<BlogResponse<List<FriendLinkDto>>> GetFriendLinksAsync(Func<Task<BlogResponse<List<FriendLinkDto>>>> func) => await Cache.GetOrAddAsync(CachingConsts.CacheKeys.GetFriendLinks(), func, CachingConsts.CacheStrategy.HALF_DAY);
    }
}