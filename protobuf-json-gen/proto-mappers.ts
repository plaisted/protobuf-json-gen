export namespace __protogen {
    export class Mapper {
        static ToJSON(objectToJson: any, props: string[], fieldMap: any) {
            const newObject: any = {};
            for (const propertyName in (objectToJson as object)) {
                if (objectToJson.hasOwnProperty(propertyName)) {
                    if (propertyName.startsWith('__')) {
                        continue;
                    }
                    if (props.indexOf(propertyName) !== -1) {
                        if (fieldMap.hasOwnProperty(propertyName)) {
                            const mapper = fieldMap[propertyName];
                            if (mapper['ToJSON'] !== undefined) {
                                newObject[propertyName] = mapper['ToJSON'](objectToJson[propertyName]);
                            } else {
                                newObject[propertyName] = new fieldMap[propertyName](objectToJson[propertyName]);
                            }
                        } else {
                            newObject[propertyName] = objectToJson[propertyName];
                        }
                    }
                }
            }
            return newObject;
        }
        static Construct(existingObject: any, newObject: any, props: string[], fieldMap: any) {
            for (const propertyName in existingObject) {
                if (existingObject.hasOwnProperty(propertyName)) {
                    if (props.indexOf(propertyName) !== -1) {
                        if (fieldMap.hasOwnProperty(propertyName)) {
                            const mapper = fieldMap[propertyName];
                            if (mapper['FromJSON'] !== undefined) {
                                newObject[propertyName] = mapper['FromJSON'](existingObject[propertyName]);
                            } else {
                                newObject[propertyName] = new fieldMap[propertyName](existingObject[propertyName]);
                            }
                        } else {
                            newObject[propertyName] = existingObject[propertyName];
                        }
                    }
                }
            }
        }
    }
    export class Timestamp {
        static FromJSON(value: string): Date {
            const b: number[] = value.split(/\D+/).map(string => Number(string));
            return new Date((Date.UTC(b[0], --b[1], b[2], b[3], b[4], b[5], b[6])));
        }

        static ToJSON(value: Date): string {
            return value.toISOString();
        }
    }
}
