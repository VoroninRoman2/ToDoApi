using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ToDoApi.Models;

namespace TodoApi.Tests
{
    public class TodoControllerTests
    {
        private TodoDbContext _context;
        private TodoController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TodoDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new TodoDbContext(options);
            _controller = new TodoController(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }

        [TestCase(1, false)] // Non-existent ID
        [TestCase(1, true)] // Existing ID
        public async Task GetTodo_ReturnsTodo_OrNotFound(int id, bool exists)
        {
            Todo exampleTodo = new Todo { Title = "Existing Task", Description = "Existing Description" };
            if (exists)
            {
                _context.Todos.Add(exampleTodo);
                _context.SaveChanges();
            }

            var result = await _controller.GetTodo(id);

            if (exists)
            {
                Assert.IsNotNull(result);
                Assert.AreEqual(exampleTodo, result.Value);

                // Perform other assertions on the objectResult.Value as needed
            }
            else
            {
                // Check if the result is NotFoundResult
                var notFoundResult = result.Result as NotFoundResult;
                Assert.IsNotNull(notFoundResult);
            }
        }

        [Test]
        public async Task GetTodos_ReturnsAllTodos()
        {
            // Arrange
            var todos = new List<Todo>
            {
                new Todo { Title = "Task 1", Description = "Description 1" },
                new Todo { Title = "Task 2", Description = "Description 2" }
            };
            _context.AddRange(todos);
            _context.SaveChanges();

            // Act
            var result = await _controller.GetTodos();

            // Assert
            Assert.AreEqual(2, result.Value.Count());
            CollectionAssert.AreEqual(todos, result.Value);
        }

        [Test]
        public async Task PostTodo_CreatesNewTodo()
        {
            // Arrange
            var newTodo = new Todo { Title = "Task 1", Description = "Description 1" };

            // Act
            var result = await _controller.PostTodo(newTodo);

            // Assert
            var createdAtActionResult = result.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdAtActionResult);

            var addedTodo = createdAtActionResult.Value as Todo;
            Assert.IsNotNull(addedTodo);

            Assert.AreEqual(newTodo.Title, addedTodo.Title);
            Assert.AreEqual(newTodo.Description, addedTodo.Description);
        }

        [Test]
        public async Task DeleteTodo_DeletesExistingTodo()
        {
            // Arrange
            var todo = new Todo { Title = "Task 1", Description = "Description 1" };
            _context.Add(todo);
            _context.SaveChanges();

            var todoToDeleteId = todo.Id;

            // Act
            var result = await _controller.DeleteTodo(todoToDeleteId);

            // Assert
            var objectResult = result as NoContentResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status204NoContent, objectResult.StatusCode);


            var deletedTodo = await _context.Todos.FindAsync(todoToDeleteId);
            Assert.IsNull(deletedTodo);
        }

        [Test]
        public async Task PutTodo_UpdatesExistingTodo()
        {
            // Arrange
            var todo = new Todo { Title = "Task 1", Description = "Description 1" };
            _context.Add(todo);
            _context.SaveChanges();

            var updatedTodo = new Todo { Id = todo.Id, Title = "Updated Task", Description = "Updated Description" };

            // Act
            var result = await _controller.PutTodo(updatedTodo.Id, updatedTodo);

            // Assert
            Assert.IsTrue((result is NoContentResult) || (result is ObjectResult objectResult && objectResult.StatusCode == StatusCodes.Status204NoContent));

            var foundTodo = await _context.Todos.FindAsync(todo.Id);
            Assert.IsNotNull(foundTodo);
            Assert.AreEqual(updatedTodo.Title, foundTodo.Title);
            Assert.AreEqual(updatedTodo.Description, foundTodo.Description);
        }
    }
}