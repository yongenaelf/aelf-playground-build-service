var JSONFile= require('fs').readFileSync("./tests/performance/requestBody.json");

exports.setJSONBody = (req, context, events, next) => {
    const requestJSON= JSON.parse(JSONFile);
    
    context.vars.requestBody = requestJSON;
    return next();
};