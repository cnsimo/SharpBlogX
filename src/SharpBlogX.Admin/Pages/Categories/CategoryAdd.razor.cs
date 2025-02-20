﻿using SharpBlogX.Dto.Blog.Params;
using SharpBlogX.Response;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharpBlogX.Admin.Pages.Categories
{
    public partial class CategoryAdd
    {
        CreateCategoryInput input = new CreateCategoryInput();

        public async Task HandleSubmit()
        {
            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Alias))
            {
                return;
            }

            var json = JsonConvert.SerializeObject(input);

            var response = await GetResultAsync<BlogResponse>("api/blog/category", json, HttpMethod.Post);
            if (response.Success)
            {
                await Message.Success("Successful", 0.5);
                NavigationManager.NavigateTo("/categories/list");
            }
            else
            {
                await Message.Error(response.Message);
            }
        }
    }
}