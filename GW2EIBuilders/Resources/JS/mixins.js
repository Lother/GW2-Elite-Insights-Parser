"use strict";

var numberComponent = {
    methods: {
        // https://stackoverflow.com/questions/16637051/adding-space-between-numbers
        integerWithSpaces: function(x) {
            return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
        },
        round: function (value) {
            if (isNaN(value)) {
                return 0;
            }
            return Math.round(value);
        },
        round1: function (value) {
            if (isNaN(value)) {
                return 0;
            }
            var mul = 10;
            return Math.round(mul * value) / mul;
        },
        round2: function (value) {
            if (isNaN(value)) {
                return 0;
            }
            var mul = 100;
            return Math.round(mul * value) / mul;
        },
        round3: function (value) {
            if (isNaN(value)) {
                return 0;
            }
            var mul = 1000;
            return Math.round(mul * value) / mul;
        }
    }
};

var graphComponent = {
    data: function () {
        return {
            graphdata: {
                dpsmode: 0,
                graphmode: logData.wvw ? GraphType.Damage : GraphType.DPS,
                damagemode: DamageType.All,
            },
            layout: {},
            dpsCache: new Map(),
            dataCache: new Map(),
        };
    },
    methods: {
            updateVisibily: function (images, x0, x1) {
                var redraw = false;
                for (var i = 0; i < images.length; i++) {
                    var image = images[i];
                    var old = image.visible;
                    image.visible = typeof x0 === "undefined" || ((image.x <= x1+15 && image.x >= x0 - 15) && (x1 - x0) < 100);
                    redraw = redraw || image.visible !== old;
                }
                return redraw;
            },
    }
};

var timeRefreshComponent = {
    props: ["time"],
    data: function () {
        return {
            refreshTime: 0
        };
    },
    computed: {
        timeToUse: function () {
            if (animator) {
                var animated = animator.animation !== null;
                if (animated) {
                    var speed = animator.speed;
                    if (Math.abs(this.time - this.refreshTime) > speed * 64) {
                        this.refreshTime = this.time;
                        return this.time;
                    }
                    return this.refreshTime;
                } else {
                    this.refreshTime = this.time;
                    return this.time;
                }
            }
            return this.time;
        },
    },
};
