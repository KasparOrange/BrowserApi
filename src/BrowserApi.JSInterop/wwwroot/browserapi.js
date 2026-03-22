(function () {
    "use strict";

    const listeners = new Map();
    let listenerId = 0;

    globalThis.browserApi = {
        getProperty(obj, name) {
            return obj[name];
        },

        setProperty(obj, name, value) {
            obj[name] = value;
        },

        invoke(obj, method, args) {
            return obj[method](...(args || []));
        },

        async invokeAsync(obj, method, args) {
            return await obj[method](...(args || []));
        },

        construct(className, args) {
            const parts = className.split(".");
            let ctor = globalThis;
            for (const part of parts) {
                ctor = ctor[part];
                if (!ctor) throw new Error(`Constructor not found: ${className}`);
            }
            return new ctor(...(args || []));
        },

        getGlobal(name) {
            return globalThis[name];
        },

        addEventListener(obj, eventName, dotNetRef) {
            const id = ++listenerId;
            const handler = function (event) {
                dotNetRef.invokeMethodAsync("OnEvent", event);
            };
            obj.addEventListener(eventName, handler);
            listeners.set(id, { obj, eventName, handler });
            return id;
        },

        removeEventListener(obj, eventName, id) {
            const entry = listeners.get(id);
            if (entry) {
                entry.obj.removeEventListener(entry.eventName, entry.handler);
                listeners.delete(id);
            }
        },

        batch(targets, commands) {
            for (const cmd of commands) {
                const t = targets[cmd.t];
                if (cmd.o === 0) {
                    t[cmd.n] = cmd.v;
                } else {
                    t[cmd.n](...(cmd.a || []));
                }
            }
        },

        queryProperty(root, selector, propName) {
            return Array.from(root.querySelectorAll(selector)).map(el => el[propName]);
        },

        queryProperties(root, selector, propNames) {
            return Array.from(root.querySelectorAll(selector)).map(el => {
                const obj = {};
                for (const p of propNames) obj[p] = el[p];
                return obj;
            });
        },

        queryElements(root, selector) {
            return Array.from(root.querySelectorAll(selector));
        },

        getProperties(target, propNames) {
            const obj = {};
            for (const p of propNames) obj[p] = target[p];
            return obj;
        }
    };
})();
