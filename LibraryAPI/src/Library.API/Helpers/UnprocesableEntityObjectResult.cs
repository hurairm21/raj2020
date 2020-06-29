using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
 

namespace Library.API.Helpers
{
    public class UnprocesableEntityObjectResult: ObjectResult
    {
        //we only need key value pair mpdel state so we accept arg of type ModelStateDictionary

        public UnprocesableEntityObjectResult(ModelStateDictionary modelState):
            base(new SerializableError(modelState))
        {
            if (modelState == null)//incase modelState sent is null, throw arg null exception
                throw new ArgumentNullException(nameof(modelState));
            StatusCode = 422;
        }
        
    }
}
