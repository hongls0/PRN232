// Trong file: MarathonManager.Web/ViewModels/AdminDashboardViewModel.cs

using MarathonManager.Web.DTOs;

using X.PagedList; // <-- Cần cho phân trang
using System.Collections.Generic;

namespace MarathonManager.Web.ViewModels
{
    // ViewModel này dùng cho trang Index (Dashboard)
    public class AdminDashboardViewModel
    {
        // Danh sách TẤT CẢ giải chạy (đã phân trang)
        public IPagedList<RaceSummaryDto> AllRaces { get; set; }

        // Danh sách TẤT CẢ người dùng (đã phân trang)
        public IPagedList<UserDto> AllUsers { get; set; }

        // Danh sách TẤT CẢ bài blog (đã phân trang)
        public IPagedList<BlogAdminDto> AllBlogPosts { get; set; }

        public AdminDashboardViewModel()
        {
            // Khởi tạo rỗng để tránh lỗi null
            AllRaces = new PagedList<RaceSummaryDto>(new List<RaceSummaryDto>(), 1, 1);
            AllUsers = new PagedList<UserDto>(new List<UserDto>(), 1, 1);
            AllBlogPosts = new PagedList<BlogAdminDto>(new List<BlogAdminDto>(), 1, 1);
        }
    }
}