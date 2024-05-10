# Playground service

## Setup

1. Go to PlaygroundService/appsettings.json, change the configuration under ContractSetting to your own file path. After that, start the service.

2. Using postman or other http client, call this endpint `localhost:7020/playground/generate` with the following params.

   ```
   {
    "ContractClass": "using AElf.Sdk.CSharp;\nusing Google.Protobuf.WellKnownTypes;\n\nnamespace AElf.Contracts.HelloWorld\n{\n    // Contract class must inherit the base class generated from the proto file\n    public class HelloWorld : HelloWorldContainer.HelloWorldBase\n    {\n        // A method that modifies the contract state\n        public override Empty Update(StringValue input)\n        {\n            // Set the message value in the contract state\n            State.Message.Value = input.Value;\n            // Emit an event to notify listeners about something happened during the execution of this method\n            Context.Fire(new UpdatedMessage\n            {\n                Value = input.Value\n            });\n            return new Empty();\n        }\n\n        // A method that read the contract state\n        public override StringValue Read(Empty input)\n        {\n            // Retrieve the value from the state\n            var value = State.Message.Value;\n            // Wrap the value in the return type\n            return new StringValue\n            {\n                Value = value\n            };\n        }\n    }\n    \n}",
    "StateClass": "using AElf.Sdk.CSharp.State;\n\nnamespace AElf.Contracts.HelloWorld\n{\n    // The state class is access the blockchain state\n    public class HelloWorldState : ContractState \n    {\n        // A state that holds string value\n        public StringState Message { get; set; }\n    }\n}",
    "Proto": "syntax = \"proto3\";\n\nimport \"aelf/options.proto\";\nimport \"google/protobuf/empty.proto\";\nimport \"google/protobuf/wrappers.proto\";\n// The namespace of this class\noption csharp_namespace = \"AElf.Contracts.HelloWorld\";\n\nservice HelloWorld {\n  // The name of the state class the smart contract is going to use to access blockchain state\n  option (aelf.csharp_state) = \"AElf.Contracts.HelloWorld.HelloWorldState\";\n\n  // Actions (methods that modify contract state)\n  // Stores the value in contract state\n  rpc Update (google.protobuf.StringValue) returns (google.protobuf.Empty) {\n  }\n\n  // Views (methods that don't modify contract state)\n  // Get the value stored from contract state\n  rpc Read (google.protobuf.Empty) returns (google.protobuf.StringValue) {\n    option (aelf.is_view) = true;\n  }\n}\n\n// An event that will be emitted from contract method call\nmessage UpdatedMessage {\n  option (aelf.is_event) = true;\n  string value = 1;\n}"
   }
   ```

3. Go to HelloWorldContract/src/bin/src/Debug/net6.0, you can see the dll.patched file has been generated.
