using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extension;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly ILikesRepository likesRepository;
        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository)
        {
            this.likesRepository = likesRepository;
            this.userRepository = userRepository;

        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username){
            var sourceUserId = User.GetUserId();
            var likedUser = await this.userRepository.GetUserByUsernameAsync(username);
            var sourceUser = await this.likesRepository.GetUserwithLikes(sourceUserId);
            if (likedUser == null) return NotFound();

            if(sourceUser.UserName == username) return BadRequest("you cannot like yourself");

        var userLike = await this.likesRepository.GetUserLike(sourceUserId,likedUser.Id);

        if(userLike!= null) return BadRequest("you already liked this user");

        userLike = new UserLike{
            SourceUserId = sourceUserId,
            LikedUserId = likedUser.Id
        };
        sourceUser.LikedUsers.Add(userLike);

        if(await this.userRepository.SaveAllAsync()) return Ok();

        return BadRequest("Failed to Like user");

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLike([FromQuery]LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            var users =  await this.likesRepository.GetUserLikes(likesParams);

         Response.AddPaginationHeader(users.CurrentPage,users.PageSize,users.TotalCount,users.TotalPages);
            return Ok(users);
        }

    }
}