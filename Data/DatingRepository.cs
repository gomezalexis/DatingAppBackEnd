using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;
        public DatingRepository(DataContext context)
        {
            _context = context;

        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(u => u.UserId == userId).FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users
                .Include(l => l.Likee)
                .Include(l => l.Liker)
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(p => p.Photos).OrderByDescending(u => u.LastActive).AsQueryable();

            users = users.Where(u => u.Id != userParams.UserId);

            users = users.Where(u => u.Gender == userParams.Gender);

            if(userParams.Likers)
            {
                var userLikers = await GetUserLikers(userParams.UserId);
                users = users.Where(u => userLikers.Any(liker => liker.LikerId == u.Id));
            }
            
            if(userParams.Likees)
            {
                // filter the users into a single
                var userLikees = await GetUserLikees(userParams.UserId);
                users = users.Where(u => userLikees.Any(likee => likee.LikeeId == u.Id));
            }

            if(userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                users = users.Where(u => u.DateOfBirth.CalculateAge() >= userParams.MinAge
                    && u.DateOfBirth.CalculateAge() <= userParams.MaxAge);
            }

            if(!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        private async Task<IEnumerable<Like>> GetUserLikees(int id)
        {
            var user = await GetUser(id);
            var userLikees = user.Liker.Where(u => u.LikerId == id).ToList();
            return userLikees;
        }

        private async Task<IEnumerable<Like>> GetUserLikers(int id)
        {
            var user = await GetUser(id);
            var userLikers = user.Likee.Where(u => u.LikeeId == id).ToList();
            return userLikers;
        }
    }
}