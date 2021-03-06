using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using TaskProcessor.Model;
using Microsoft.EntityFrameworkCore;

namespace TaskProcessor.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class TaskProcessorController : ControllerBase
    {

        private readonly TaskDbContext _context;


        public TaskProcessorController(TaskDbContext context)
        {
            _context = context;
        }

        private readonly ILogger _logger;
        private IConnection _connection;
        private IModel _channel;
        //public IMapper _mapper { get; }



        // GET: api/TaskProcessor/5
        /// <summary>
        /// Reads all messages in task queue and save
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Consumes("application/json")]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
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

            // set 1 message at a time to read
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                // received message  
                var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

                //Console.WriteLine($"consumer received {content}");
                Console.WriteLine("content in handle string" + content);

                // Save value to database
                //TaskItem task = _mapper.Map<TaskItem>(content);
                //TaskItem task = JsonConvert.DeserializeObject<TaskItem>(content);
                var data = JsonConvert.DeserializeObject<TaskItem>(content);
                var myMessage = new TaskItem();
                myMessage.customerID = data.customerID;
                myMessage.description = data.description;
                myMessage.priority = data.priority;
                myMessage.status = data.status;
                myMessage.taskID = data.taskID;
                Console.WriteLine($"consumer recevied {myMessage}");

                _context.Task.Add(myMessage);
                _context.SaveChanges();

                //HandleMessage(content);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume("tasks", false, consumer);

            //return "Completed";
            //return Ok(await _context.Task.ToListAsync());
            return await _context.Task.ToListAsync();

        }


        // GET: api/TaskProcessor
        //[HttpGet("{id}", Name = "Get")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public string GetTaskById(int id)
        //{

        //    return "Completed";

        //}

        private async void HandleMessage(string content)
        {

            //Console.WriteLine($"consumer received {content}");
            Console.WriteLine("content in handle string" + content);

            // Save value to database
            //TaskItem task = _mapper.Map<TaskItem>(content);
            TaskItem task = JsonConvert.DeserializeObject<TaskItem>(content);
            _context.Add(task);
            _context.SaveChanges();


            if (task.taskID == 1001)
            {
                task.status = StatusTypes.FAILED; // set status of task id to 1001 to fail status and publish to another queue;
            }

            Console.WriteLine("READ FROM DB");
            var itemList = await _context.Task.ToArrayAsync();
            
            Console.WriteLine(itemList);
            Console.WriteLine(task.status);
            // we just print this message   
            //_logger.LogInformation($"consumer received {content}");
            //Console.WriteLine($"consumer received {content}");
        }


        private void HandleFailStatus(string responseString)
        {

            var jObject = JObject.Parse(responseString);

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "process",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // string message = user.task;
                string message = JsonConvert.SerializeObject(jObject); // this is to pass response obj

                var body = Encoding.UTF8.GetBytes(message);
                Console.WriteLine(body);

                channel.BasicPublish(exchange: "",
                                    routingKey: "process",
                                    basicProperties: null,
                                    body: body);
            }
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }


        //POST: api/TaskProcessor
        /// <summary>
        /// Qn5 SAGA: Posts item to task-processed queue
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<TaskItem>> PostTaskStatus(TaskItem taskItem)
        {

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
                channel.QueueDeclare(queue: "task-processed",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // string message = user.task;
                string message = JsonConvert.SerializeObject(taskItem); // this is to pass response obj

                var body = Encoding.UTF8.GetBytes(message);
                Console.WriteLine(body);

                channel.BasicPublish(exchange: "",
                                    routingKey: "task-processed",
                                    basicProperties: null,
                                    body: body);
            }
            return NoContent();
        }

        //// PUT: api/TaskProcessor/5
        //[HttpPut("{id}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<IEnumerable<TaskItem>>> PutTask(TaskItem taskItem)
        //{

        //    return NoContent()
        //}

        // DELETE: api/TaskProcessor/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
