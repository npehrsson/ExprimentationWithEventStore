using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;

namespace Tables.Controllers {
    [Route("")]
    public class TablesController : Controller {

        // GET: api/values
        [HttpGet("create")]
        public async Task<string> Create() {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();
                var writer = new EventWriter(connection, new CustomSerializer());
                var createdEvent = new TableCreatedEvent(Guid.NewGuid(), "Table rule 1");
                await writer.Write("tables-" + createdEvent.TableId.ToString("N"), createdEvent);

                return createdEvent.TableId.ToString("N");
            }
        }

        // GET api/values/5
        [HttpGet("/get/{id}")]
        public async Task<JsonResult> Get(Guid id) {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();

                var query = new TableQuery(connection);

                var result = await query.SingleWithBatchLoop(id, 0);

                return new JsonResult(null);
            }
        }

        // GET api/values/5
        [HttpGet("/get2/{id}")]
        public async Task<JsonResult> Get2(Guid id)
        {
            var watch = Stopwatch.StartNew();
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();
              
                var query = new TableQuery(connection);

                var result = query.SingleWithSubscription(id, 0);

                var accessor = new HttpContextAccessor();
                var total = watch.ElapsedMilliseconds;
                var json = (TimeSpan)accessor.HttpContext.Items["TotalJson"];
                var binary = (TimeSpan)accessor.HttpContext.Items["TotalBinary"];

                accessor.HttpContext.Response.Headers.Add("Json", json.TotalMilliseconds.ToString());
                accessor.HttpContext.Response.Headers.Add("Binary", binary.TotalMilliseconds.ToString());
                accessor.HttpContext.Response.Headers.Add("Total", total.ToString());

                return new JsonResult(null);
            }
        }

        [HttpGet("/get3/{id}")]
        public async Task<JsonResult> Get3(Guid id) {
            var watch = Stopwatch.StartNew();
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();

                var query = new TableQuery(connection);

                var result = await query.SingleReadAllEventsAndThenAppned(id, 0);

                var accessor = new HttpContextAccessor();
                var total = watch.ElapsedMilliseconds;
                var json = (TimeSpan)accessor.HttpContext.Items["TotalJson"];
                var binary = (TimeSpan)accessor.HttpContext.Items["TotalBinary"];

                accessor.HttpContext.Response.Headers.Add("Json", json.TotalMilliseconds.ToString());
                accessor.HttpContext.Response.Headers.Add("Binary", binary.TotalMilliseconds.ToString());
                accessor.HttpContext.Response.Headers.Add("Total", total.ToString());

                return new JsonResult(null);
            }
        }

        [HttpGet("/import")]
        public async Task<string> Import(int rows, int columns) {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();
                var writer = new EventWriter(connection, new CustomSerializer());

                var events = new List<IEvent>();

                var createdEvent = new TableCreatedEvent(Guid.NewGuid(), "Table rule 1");
                events.Add(createdEvent);

                var columnList = new List<Guid>();

                for (var column = 0; column < columns; column++) {
                    var columnId = Guid.NewGuid();
                    events.Add(new CreatedColumnEvent(columnId, "Column " + column));
                    columnList.Add(columnId);
                }

                for (var row = 0; row < rows; row++) {
                    var rowEvent = new CreatedRowEvent(Guid.NewGuid());
                    events.Add(rowEvent);

                    foreach (var column in columnList) {
                        events.Add(new CellEditedEvent(rowEvent.RowId, column, "Data"));
                    }
                }

                await writer.Write("tables-" + createdEvent.TableId.ToString("N"), events);

                return createdEvent.TableId.ToString("N");
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value) {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value) {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
        }
    }
}
