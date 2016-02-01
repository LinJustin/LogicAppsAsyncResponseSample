﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace AsyncResponse.Controllers
{
    public class AsyncController : ApiController
    {
        //State dictionary for sample - stores the state of the working thread
        private static Dictionary<Guid, bool> runningTasks = new Dictionary<Guid, bool>();


        /// <summary>
        /// This is the method that starts the task running.  It creates a new thread to complete the work on, and returns an ID which can be passed in to check the status of the job.  
        /// In a real world scenario your dictionary may contain the object you want to return when the work is done.
        /// </summary>
        /// <returns>HTTP Response with needed headers</returns>
        [HttpPost]
        [Route("api/startwork")]
        public async Task<HttpResponseMessage> longrunningtask()
        {
            Guid id = Guid.NewGuid();  //Generate tracking Id
            runningTasks[id] = false;  //Job isn't done yet
            new Thread(() => doWork(id)).Start();   //Start the thread of work, but continue on before it completes
            HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);   
            responseMessage.Headers.Add("location", String.Format("/api/status/{0}", id));  //Where the engine will poll to check status
            responseMessage.Headers.Add("retry-after", "20");   //How many seconds it should wait (20 is default if not included)
            return responseMessage;
        }


        /// <summary>
        /// This is where the actual long running work would occur.
        /// </summary>
        /// <param name="id"></param>
        private void doWork(Guid id)
        {
            Debug.WriteLine("Starting work");
            Task.Delay(10000).Wait(); //Do work
            Debug.WriteLine("Work completed");
            runningTasks[id] = true;  //Set the flag to true - work done
        }

        /// <summary>
        /// Method to check the status of the job.  This is where the location header redirects to.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/status/{id}")]
        public HttpResponseMessage checkStatus([FromUri] Guid id)
        {
            if(runningTasks.ContainsKey(id) && runningTasks[id])
            {
                runningTasks.Remove(id);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else if(runningTasks.ContainsKey(id))
            {
                HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);
                responseMessage.Headers.Add("location", String.Format("/api/status/{0}", id));
                responseMessage.Headers.Add("retry-after", "20");
                return responseMessage;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, new Exception("No job exists with the specified id"));
            }
        }
    }
}