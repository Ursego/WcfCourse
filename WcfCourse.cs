// ASP.NET is a web application framework by Microsoft for building dynamic web sites, services, and APIs using .NET.

// WCF (Windows Communication Foundation) is a .NET framework for building service-oriented applications that communicate over various protocols like HTTP, TCP, and named pipes.

// Often, they are used together - WCF services can be hosted inside ASP.NET applications using svc files.
// An .svc file is similar in purpose to an .aspx file in ASP.NET — it's an endpoint handler used to host a WCF service in IIS or ASP.NET.
// It maps incoming HTTP requests to the corresponding WCF service class.
// Structure of a .svc file:
<%@ ServiceHost Language= "C#" Service="MyNamespace.MyService" %>
// ServiceHost directive tells ASP.NET to use WCF's HTTP handler.
// Service="..." specifies the fully qualified name of the service class to be instantiated.

// Now, let's write a simple WCF application.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// DATA TRANSFER OBJECT
// CustomerModule/Dtos/CustomerDto.cs
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// The class whose purpose is to contain customer-related data for transmission between different layers of your application.

using System;

namespace MichaelZuskinWcfCourse.CustomerModule.Dtos // is many real applications, the folder can be named Model or Domain rater than Dtos
{
    public class CustomerDto
    {
        public int? customerId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? updatedBy { get; set; }
        public DateTime? updatedAt { get; set; }
    }
}

// CustomerDto represents a structured format for customer information that can be safely transmitted over the network.
// You use DTOs like `CustomerDto` to decouple internal data structures from external consumers and control the shape and contents of the transferred data.

// It's serialization-friendly - all the properties are public and have simple data types, which makes the class easily serializable by WCF for SOAP or REST communication.

// The use of nullable types provides flexibility and helps handle partial CRUD operation, especially with fields auto-generated by the DB.

// This class will be used as:
//    * An input parameter for INSERT and UPDATE service methods.
//    * A return type from SELECT service methods.

// Notice the folders structure:
namespace MichaelZuskinWcfCourse.CustomerModule.Dtos
// A module is usually a screen which contains a few components (data areas). That corresponds to the terminology of Angular.
// Usually, each module has a dedicated folder (like our CustomerModule).
// Our module (the Customer Screen) could contain such components as Customers List (for search), Customer Form, Address etc.
// CustomerDto would be the main DTO for the Customers List (you need an array of them) and the Customer Form components.
// For other components, MichaelZuskinWcfCourse folder would contain other ...Dto classes such as CustomerAddressDto.
// This logic works also for the data types which you will learn next.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SERVICE CONTRACT
// CustomerModule/Contracts/ICustomerContract.cs
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// A contract interface defines operations exposed by the web service, i.e. the methods that clients can remotely invoke over the network when interacting with the service.
// It's the contract between the WCF service and the client (front end).

using System.Collections.Generic;
using System.ServiceModel;
using MichaelZuskinWcfCourse.CustomerModule.Dtos;

namespace MichaelZuskinWcfCourse.CustomerModule.Contracts
{
    [ServiceContract]
    public interface ICustomerContract
    {
        [OperationContract]
        List<CustomerDto> SelCustomerList(string lastName); // SELECT the list of customers whose last name includes the provided search parameter

        [OperationContract]
        CustomerDto SelCustomer(int customerId); // SELECT a singde customer by its ID

        [OperationContract]
        CustomerDto InsCustomer(CustomerDto cst); // INSERT a customer

        [OperationContract]
        CustomerDto UpdCustomer(CustomerDto cst); // UPDATE a customer.

        [OperationContract]
        int DelCustomer(int customerId); // DELETE a customer.
    }
}

// The [ServiceContract] attribute is required to mark an interface as a WCF service contract.
// It tells the WCF runtime that the interface defines a set of operations that are available to clients.
// This attribute ensures the service is discoverable, and can be consumed by various clients regardless of platform.
// Without it, WCF will not recognize the interface as a valid contract for service communication.

// Each method within the interface that should be exposed to clients must be decorated with the [OperationContract] attribute.
// This attribute indicates that the method is part of the service contract and can be invoked by clients remotely.
// Methods without this attribute will not be exposed to the client, even if they are defined in the interface.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// THE DATABASE CONTROLLER
// CustomerModule/DbControllers/CustomerDbController.cs
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// The the class which directly calls the stored procedures.
// There are many libraries for working with databases. Our example uses Argon, but you could work with other libraries.
// In any case, this file gives you a clear idea of ​​the DB endpoint of a WCF web service.
// Our class communicates with Oracle, so the names of the stored procedures are used with the package name via dot notation.

using System;
using System.Collections.Generic;
using Argon.Core;
using Argon.Core.DbCommandParameters;
using MichaelZuskinWcfCourse.CustomerModule.Contracts;
using MichaelZuskinWcfCourse.CustomerModule.Dtos;

namespace MichaelZuskinWcfCourse.CustomerModule.DbControllers
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class CustomerDbController : ICustomerContract
    {
        const string dbPackage = "pkg_customer";
        private readonly IArgonDbProviderFactory _dbProviderFactory = new ArgonDbProviderFactory();

        public List<CustomerDto> SelCustomerList(string lastName)
        {
            var procParams = new List<DbCommandParameter>
            {
                new DbCommandParameter("i_last_name", DbCommandParameterType.String, lastName, ParameterDirection.Input)
            };
            return ExecProc("sel_customer_list", procParams);
        }

        public CustomerDto SelCustomer(int customerId)
        {
            var procParams = new List<DbCommandParameter>
            {
                new DbCommandParameter("i_customer_id", DbCommandParameterType.Number, customerId, ParameterDirection.Input)
            };
            var res = ExecProc("sel_customer", procParams);
            return res.Count > 0 ? res[0] : null;
        }

        public CustomerDto InsCustomer(CustomerDto cst)
        {
            var procParams = GenerateSaveParams(cst);
            var res = ExecProc("ins_customer", procParams);
            return res.Count > 0 ? res[0] : null;
        }

        public CustomerDto UpdCustomer(CustomerDto cst)
        {
            var procParams = GenerateSaveParams(cst);
            var res = ExecProc("upd_customer", procParams);
            return res.Count > 0 ? res[0] : null;
        }

        public int DelCustomer(int customerId)
        {
            var proc = $"{dbPackage}.del_customer";
            var procParams = new List<DbCommandParameter>
            {
                new DbCommandParameter("i_customer_id", DbCommandParameterType.Number, customerId, ParameterDirection.Input),
            };
            _dbProviderFactory.GetDbProvider().ExecuteProc(proc, procParams);
        }

        private List<DbCommandParameter> GenerateSaveParams(CustomerDto cst)
        // An extract from InsCustomer and UpdCustomer which populates parameters for save procs.
        {
            return new List<DbCommandParameter>
            {
                new DbCommandParameter("i_customer_id", DbCommandParameterType.Number, cst.customerId, ParameterDirection.Input),
                new DbCommandParameter("i_first_name", DbCommandParameterType.String, cst.firstName, ParameterDirection.Input),
                new DbCommandParameter("i_last_name", DbCommandParameterType.String, cst.lastName, ParameterDirection.Input)
            };
        }

        private List<CustomerDto> ExecProc(string proc, List<DbCommandParameter> procParams)
        // An extract from SelCustomerList, SelCustomer, InsCustomer and UpdCustomer which calls the stored proc.
        {
            procParams.Add(new DbCommandParameter("o_ref_cur", DbCommandParameterType.RefCursor, ParameterDirection.Output));
            var ret = new List<CustomerDto>();
            using (var reader = _dbProviderFactory.GetDbProvider().ExecuteProcReturnReader($"{dbPackage}.{proc}", procParams))
            {
                while (reader.Read())
                {
                    var cst = new CustomerDto
                    {
                        customerId = reader.GetInt32("customer_id").Value,
                        firstName = reader.GetString("first_name"),
                        lastName = reader.GetString("last_name"),
                        updatedBy = reader.GetString("updated_by"),
                        updatedAt = reader.GetDateTime("updated_at").Value
                    };
                    ret.Add(cst);
                }
            }
            return ret.ToArray();
        }
    }
}
// The purpose of the `CustomerDbController` class is to serve as the WCF service's backend implementation that connects the service contract to the database.
// It isolates all database operations behind a clean interface and hides the complexity of stored procedure calls, parameter management, and result parsing.
// By doing so, it allows the rest of the application to interact with customer data through simple method calls, without knowing anything about how or where the data is stored.
// This promotes separation of concerns: the service contract defines what can be done, and this class defines how it is done at the database level.

// @@@ InstanceContextMode

// Notice this attribute in the beginning of the class:
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
// It controls how service instances are created and managed.
// PerCall means that a new instance of your service class is created for every client request (i.e., each time a client calls a service method).
// After the method call completes, the instance is destroyed (garbage-collected).
// Since service instances do not retain state between calls, you need to persist state across calls using mechanisms (e.g., a DB or caching in static variables).
// The characteristics of PerCall:
// * Thread-safe by default: Since each call gets a new instance, there's no shared state to synchronize.
// * Highly scalable: Stateless instances allow easy scaling in load-balanced environments (e.g., cloud deployments).
// * Resource Efficiency: Resources (e.g., database connections) are held only for the duration of a single call, reducing memory leaks and long-term resource consumption.
// * Works with or without sessions (e.g., WSHttpBinding with sessions enabled). Even if a client uses the same proxy for multiple calls, each call gets a new service instance.

// The InstanceContextMode enumeration in WCF has two more possible values - PerSession and Single.

// PerSession:
//      Behavior: A single service instance is created per client session. The same instance handles all requests from the same client.
//      State: Stateful (retains client-specific state across calls).
//      Use Case: Stateful workflows (e.g., shopping carts, multi-step processes).
//      Lifetime: Instance lives until the session times out or closes.

// Single:
//      Behavior: A single service instance handles all requests from all clients. The instance is created when the service host opens.
//      State: Global state (shared across all clients).
//      Use Case: Shared resources (e.g., global cache, hardware access).

// When to Use Which:
//      Use PerCall for REST-like stateless services (RESTfull APIs).
//      Use PerSession for traditional stateful workflows (e.g., user login sessions).
//      Use Single sparingly for shared global state (e.g., real-time dashboards).

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// THE BASE API CONTROLLER (HTTP ENDPOINT)
// WebApi/Infrastructure/BaseApiController (inherited from ApiController)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// @@@ What is an HTTP endpoint (aka Web API)?

// An endpoint defines where and how a WCF service can be accessed, telling clients "where to find the service and how to talk to it".

// Programmatically, an endpoint is a class through which the service exposes its contract.
// That makes the service accessible to HTTP consumers, while maintaining separation of concerns and leveraging the existing WCF contract for data access and manipulation.

// An endpoint has:
// * An address which is a URL specifying where the endpoint can be accessed. Can include simple parameters (such as an entity ID).
// * Data to transfer (not included in the URL). Usually, it's a DTO.
//          It's optional. For example, data is passed for INSERT and UPDATE since it would be impractical and unsafe to pass it within the URL.
//          But it's not passed for a DELETE or a single-entity SELECT, when the service needs only the ID, which is easy to include in the URL.

// @@@ ApiController

// ApiController is the class used to build HTTP endpoints.
// Strictly speaking, it's not part of WCF - it belongs to the ASP.NET Core framework (more precisely, ASP.NET Web API) which is used in our WCF application. 
// Descendants of ApiController are designed to:
// * Handle incoming HTTP requests (GET, POST, PUT, DELETE).
// * Return data to the callers in formats like JSON or XML (today, it's usually JSON).

// Key features include:
// * Enforces Attribute Routing: Requires attribute routing, making routing definitions explicit and clear.
// * Binding Source Parameter Inference: Infers binding sources for action parameters, such as [FromBody] for complex types.
// * Automatic Model State Validation: Validates the model state and automatically returns a 400 Bad Request response if the model state is invalid.
// * Problem Details Responses: Provides detailed error responses conforming to the Problem Details standard (application/problem+json).

// Essentially, it serves as the heart of your application's API, directing and processing requests and ensuring the correct data is sent back to the client.
// When creating APIs that should follow RESTful principles, using ApiController ensures that best practices and conventions are automatically applied.

// @@@ BaseApiController

// The application must provide an ApiController's descendant (usually named BaseApiController) to be inherited by concrete business API controllers.

// The purpose of creating a BaseApiController is to establish a common foundation for all business API controllers.

// BaseApiController:
// * Separates framework concerns (ApiController) from application-specific concerns (BaseApiController).
// * Makes the codebase more maintainable and testable.
// * Allows for easier refactoring and updates to common functionality.

// Typical tasks of BaseApiController:
// * Authentication and authorization logic that applies to all controllers.
// * Security policies (like checking user permissions).
// * Logging and auditing mechanisms.
// * Performance monitoring and metrics collection.
// * Common error handling and exception management.
// * Input validation patterns.
// * Response formatting and standardization.
// * Consistency:
//         Ensures all controllers follow the same patterns and conventions.
//         Provides a consistent API experience for consumers.
// * Maintainability:
//         Makes it easier to modify behavior across all controllers from one place.
//         Reduces code duplication by implementing shared logic once.

// Now I'll give you a hypothetical example of such a class.
// You don't need to study it in detail - it only gives you a general idea.
// Instead, look at how a real similar class works in the application you're working on.
// In fact, you don't even need to do that - you just need to know that the API controllers you'll be creating must inherit from BaseApiController.

using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace MichaelZuskinWcfCourse.WebApi.Infrastructure // notice that it's not within CustomerModule - it's an app-wide class used by many modules
{
    public class BaseApiController : ApiController // !!!!!!! inherited from ApiController !!!!!!!
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            ValidateServiceCallAuthorization();
            LogServiceCall();
        }

        private void ValidateServiceCallAuthorization()
        {
            var user = this.ControllerContext.RequestContext.Principal;
            var userName = user?.Identity?.Name;
            var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;
            var userRoles = user?.Identity as System.Security.Claims.ClaimsIdentity;
            var url = this.ControllerContext.Request.RequestUri.ToString();
            var httpMethod = this.ControllerContext.Request.Method.Method;
            var headers = this.ControllerContext.Request.Headers;
            var routeData = this.ControllerContext.RouteData;
            var controllerName = routeData.Values["controller"]?.ToString();
            var actionName = routeData.Values["action"]?.ToString();
            var clientIpAddress = this.ControllerContext.Request.GetClientIpAddress();

            var isAuthorized = ... // implementation of authorization check would go here...
            if (!isAuthorized)
            {
                var err = $"Service call not authorized. URL: {url}, User: {userName}, Controller: {controllerName}, Action: {actionName}, Method: {httpMethod}, IP: {clientIpAddress}";
                throw new UnauthorizedAccessException(err);
            }
        }

        private void LogServiceCall()
        {
            var user = this.ControllerContext.RequestContext.Principal;
            var userName = user?.Identity?.Name;
            var url = this.ControllerContext.Request.RequestUri.ToString();
            var httpMethod = this.ControllerContext.Request.Method.Method;
            var headers = this.ControllerContext.Request.Headers;
            var requestContent = this.ControllerContext.Request.Content;
            var routeData = this.ControllerContext.RouteData;
            var controllerName = routeData.Values["controller"]?.ToString();
            var actionName = routeData.Values["action"]?.ToString();
            var clientIpAddress = this.ControllerContext.Request.GetClientIpAddress();
            var userAgent = headers.UserAgent?.ToString();
            var timestamp = System.DateTime.UtcNow;
            var requestId = System.Guid.NewGuid();

            // implementation of logging would go here...
        }
    }
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// THE CONCRETE BUSINESS API CONTROLLER (HTTP ENDPOINT)
// CustomerModule/ApiControllers/CustomerApiController (inherited from BaseApiController)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// The next CustomerApiController class serves as the HTTP endpoint handler for your WCF service.
// It acts as a thin layer between HTTP and the customer domain logic.
// The class parses incoming URLs and maps them to specific service methods (in our example, it delegates processing to CustomerDbController).
// After the methods invocation, returns results to the client using IHttpActionResult with proper:
//     Status codes (200 OK, 400 Bad Request, etc.).
//     Serialized output (JSON/XML).

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using MichaelZuskinWcfCourse.CustomerModule.DbControllers; // CustomerDbController
using MichaelZuskinWcfCourse.CustomerModule.Dtos; // CustomerDto
using MichaelZuskinWcfCourse.WebApi.Infrastructure; // BaseApiController

namespace MichaelZuskinWcfCourse.CustomerModule.ApiControllers
{
    [RoutePrefix("customers")]
    public class CustomerApiController : BaseApiController // !!!!!!! inherited from BaseApiController !!!!!!!
    {
        private readonly CustomerDbController _dbController;

        public CustomerApiController(CustomerDbController dbController)
        {
            _dbController = dbController;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult SelCustomerList(string lastName)
        {
            List<CustomerDto> result = _dbController.SelCustomerList(lastName);
            return Ok(result);
        }

        [HttpGet]
        [Route("{customerId:int}")]
        public IHttpActionResult SelCustomer(int customerId)
        {
            CustomerDto result = _dbController.SelCustomer(customerId);
            return Ok(result);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult InsCustomer([FromBody] CustomerDto cst)
        {
            CustomerDto result = _dbController.InsCustomer(cst);
            return Ok(result);
        }

        [HttpPut]
        [Route("{customerId:int}")]
        public IHttpActionResult UpdCustomer([FromBody] CustomerDto cst)
        {
            CustomerDto result = _dbController.UpdCustomer(cst);
            return Ok(result);
        }

        [HttpDelete]
        [Route("{customerId:int}")]
        public IHttpActionResult DelCustomer(int customerId)
        {
            int result = _dbController.DelCustomer(customerId);
            return Ok(result);
        }
    }
}

// The CustomerApiController class exposes customer-related operations over HTTP in a RESTful manner.
// It acts as an HTTP-based interface between external clients and the underlying WCF service represented by the CustomerDbController.
// The controller handles incoming HTTP requests, maps them to corresponding WCF service methods, and returns structured HTTP responses.

// Each public method in the controller corresponds to a specific HTTP verb (GET, POST, PUT, DELETE) and performs one of the CRUD operations on customer data.
// These methods use attributes like [HttpGet], [HttpPost], [HttpPut], and [HttpDelete] to indicate the type of HTTP request they handle.
// Note using [HttpPost] for INSERT and [HttpPut] for UPDATE.
// The [Route] defines the specific URL pattern that triggers each method.
// The controller is marked with [RoutePrefix("customers")], which means all routes are prefixed with "customers".

// The controller relies on dependency injection to receive an instance of CustomerDbController, which it uses to delegate the actual business logic and data operations.

// @@@ Ok()

// The `Ok()` function is defined in the `ApiController` class, which is part of the `System.Web.Http` namespace in ASP.NET Web API.
// It is a protected method with a return type of `IHttpActionResult`.
// The purpose of `Ok()` is to create an HTTP response with status code 200 (OK) and optionally include a response body.
// It is used to indicate that a request was successful and to return data to the client.
// The data is serialized using content negotiation, meaning it will be formatted as JSON, XML, or another supported type depending on the client's request headers.
// Internally, `Ok()` creates an instance of `OkNegotiatedContentResult<T>`, which instructs the Web API framework to return the appropriate response.

// You could ask: "Why do the methods always return success? Errors do happen!"
// In fact, besides ok(), there are some more function you can use:

return BadRequest("Wrong input data"); // 400 Bad Request
// It could be used to validate parameters, but we leave that task to the last routine in the call chain - the stored procedure ().

return NotFound(); // 404 Not Found
// Is not usually used since, in this situation, we normally return an empty data structure - a DTO with blank fields or an array with zero elements.
// We don't want to involve an error mechanism, considering this as a business situation.
// The client (front end) must check that return and decide what to do if it's blank.

return InternalServerError(exeption); // 500 Internal Server Error
                                      // Handled automatically by ASP.NET. This avoids repeating try/catch blocks in every methof of the controller.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// INJECTING THE DB CONTROLLER INTO THE API CONTROLLER
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// As you've seen, an instance of CustomerDbController is injected into CustomerApiController via the constructor:
public CustomerApiController(CustomerDbController dbController)
{
    _dbController = dbController;
}
// To make this happen, you have to take some steps.
// Since these vary significantly depending on the middle tier architecture you are using, we won't give an example,
// but typically this involves registering the object to inject in some configuration file.
// If you've started working in a WCF project, see how this is done by searching for the class name of the injected object of some existing module.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// An alternative architecture of your WFC application
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// In our example, there are two layers within the web service:

// CustomerApiController
// ↓
// CustomerDbController

// This assumes that all business logic is in stored procedures, so the middle tier performs purely transportation functions.
// This is the preferrable architecture - using stored procedures for BL is the most efficient way (you can access DB tables without producing extra trafic).
// However, some systems encode business logic on the .Net side (in whole or in part), so an additional layer between the API and DB controllers is needed there.

// The class could be named following the pattern <Entity>Bl, so we could name ours CustomerBl.
// It goes without saying that it should implement ICustomerContract.

// The architecture would turn into the following:

// CustomerApiController
// ↓
// CustomerBl
// ↓
// CustomerDbController

// So, CustomerBl (not CustomerDbController) would be injected now into CustomerApiController, and CustomerDbController would be injected into CustomerBl.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Service Host XML File (customer.svc) - SOAP Endpoint
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Purpose: Declares the service implementation for IIS hosting.

<% --File: CustomerService.svc --%>
<%@ ServiceHost Language="C#" 
                Service="MichaelZuskinWcfCourse.Services.CustomerDbController" 
                Factory="System.ServiceModel.Activation.WebServiceHostFactory" %>