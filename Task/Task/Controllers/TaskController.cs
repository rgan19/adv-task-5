using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Task.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client.Events;

namespace Task.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {

        private readonly TaskDBContext _context;
        public TaskController(TaskDBContext context)
        {
            _context = context;
        }
        private IConnection _connection;
        private IModel _channel;

        // GET: api/Task
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }

        // POST: api/Task
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Consumes("application/json")]
        public async Task<ActionResult<TaskItem>> Post(TaskItem taskItem)
        {
            _context.Tasks.Add(taskItem);

            string json = JsonConvert.SerializeObject(taskItem);
            Console.WriteLine("JSON");
            Console.WriteLine(json);
            //var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "tasks",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // string message = user.task;
                string message = JsonConvert.SerializeObject(taskItem); // this is to pass response obj
                
                var body = Encoding.UTF8.GetBytes(message);
                Console.WriteLine(body);

                channel.BasicPublish(exchange: "",
                                    routingKey: "tasks",
                                    basicProperties: null,
                                    body: body);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Post), new {id = taskItem.taskID}, taskItem);
        }

        [HttpPut]
        public async Task<ActionResult<IEnumerable<TaskItem>>> UpdateStatus()
        {
            // each get only gets the latest rabbitmq value?


            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);

            // create connection  
            _connection = factory.CreateConnection();

            // create channel  
            _channel = _connection.CreateModel();

            //_channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
            _channel.QueueDeclare("tasks", true, false, false, null);


            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                // received message  
                var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

                Console.WriteLine($"consumer received {content}");

                var data = JsonConvert.DeserializeObject<TaskItem>(content);
                var myMessage = new TaskItem();
                myMessage.customerID = data.customerID;
                myMessage.description = data.description;
                myMessage.priority = data.priority;
                myMessage.status = data.status;
                myMessage.taskID = data.taskID;
                Console.WriteLine($"consumer recevied {myMessage}");

                _context.Tasks.Add(myMessage);
                _context.SaveChanges();

                //HandleMessage(content);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume("task-processed", false, consumer);

            return await _context.Tasks.ToListAsync();
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }
    }
}
