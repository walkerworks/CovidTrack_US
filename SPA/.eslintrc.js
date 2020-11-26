module.exports = {
    "env": {
        "browser": true,
        "es6": true,
        "node": true
    },
    "extends": [
        "eslint:recommended",
        "plugin:vue/essential"
    ],
    "parserOptions": {
        "parser": "@typescript-eslint/parser",
        "sourceType": "module"
    },
    
    "plugins": ["@typescript-eslint"], 
    "rules": {
        "@typescript-eslint/no-unused-vars": ["warn"],
        "eqeqeq": ["error", "always"],
        "brace-style": ["warn", "stroustrup", {"allowSingleLine": true}],
        // "brace-style": ["warn"],
        "prefer-template": ["warn"],
        "indent": ["warn", 2, {"SwitchCase": 1}],
       // "linebreak-style": ["warn", "unix"],
        "no-console": "off",
        "no-debugger": "off",
        "no-unused-vars": ["warn", "all"],
        "no-trailing-spaces": ["warn"],
        "prefer-arrow-callback": ["warn", { "allowUnboundThis": false }],
        "quotes": ["warn", "single"],
        "semi": ["warn", "always"],
        "spaced-comment": ["warn", "always", {
            "line": {
                "markers": ["/"],
                "exceptions": ["-", "+"]
            },
            "block": {
                "markers": ["!"],
                "exceptions": ["*"],
                "balanced": true
            }
        }]
    }
};