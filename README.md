# protobuf-json-gen
C# helper application for creating Typescript classes from C# protobuf IMessages meant to handle JSON encoded protobuf messages.

Work in progress.

Current javascript / typescript libraries (google's, protobuf.js) for protobufs are unable to handle "JSON encoded" protobufs (https://developers.google.com/protocol-buffers/docs/proto3#json).  They also generally do not handle Google's "well known types" (wrappers, timestamps, etc.) well.

This is a static code generator to create Typescript classes capable of converting the JSON encoded protobuf messages into typed objects. At some point it will make sense to port this to a protoc plugin in C++ instead of relying on the c# generated code and reflection libraries (or merge the JSON encoded functionality with another implementation, eg. protobuf.js).

Given a protobuf like:
```
message Item {
    string item_id = 1;
    google.protobuf.StringValue optional_string = 2;
    google.protobuf.Timestamp timestamp = 2;
}
```

This will create a typescript file containing:
```typescript
import { __protogen } from './proto-mappers';

export interface IItem {
    itemId: string;
    optionalString?: string;
    timestamp?: Date;
}

export class Item implements IItem {
    itemId: string;
    optionalString?: string;
    timestamp?: Date;

    constructor(protoJson?: any) {
        if (protoJson === undefined) {
            return;
        }
        __protogen.Mapper.Construct(protoJson, this, __props_Item, __maps_Item);
    }

    static FromInterface(object: IItem): Item {
        return __protogen.Mapper.FromInterface<Item, IItem>(object, Item);
    }

    toJSON() {
        return __protogen.Mapper.ToJSON(this,  __props_Item, __maps_Item);
    }

}
const __props_Item: string[] = ['itemId', 'optionalString', timestamp];
const __maps_Item = { billDate: __protogen.Timestamp };
```

The constructor is used to create the object from the JSON encoded version of the protobuf. The contstructor maps the fields from their JSON encoded type (eg. string for Timestamp) to their logical Typescript conterpart (eg. Date for Timestamp). The static method `FromInterface` is used to create the class from a javascript object with the logical corresponding types (eg. Date for Timestamp).
