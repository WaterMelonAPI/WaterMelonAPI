﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WaterMelon_API.Models;

namespace WaterMelon_API.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        
        public UserService(IUserDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>(settings.UsersCollectionName);
        }

        public List<User> Get() => _users.Find(user => true).ToList();

        public User Get(String id) =>
            _users.Find<User>(user => user.Id == id).FirstOrDefault();

        public User GetFromIds(String username, String password)
        {
            User user = _users.Find<User>(user => user.Username.ToLower().Equals(username.ToLower()) && user.Password.ToLower().Equals(password.ToLower())).FirstOrDefault();
            return user;
        }

        public User Create(User user)
        {
            _users.InsertOne(user);
            return user;
        }

        public void Update(String id, User userIn)
        {
            _users.ReplaceOne(user => user.Id == id, userIn);
        }

        public void Remove(User userIn)
        {
            _users.DeleteOne(user => user.Id == userIn.Id);
        }

        public void Remove(String id)
        {
            _users.DeleteOne(user => user.Id == id);
        }
    }
}
