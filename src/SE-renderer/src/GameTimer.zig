

seconds_per_count: f64,
delta_time: f64,

base_time:   i64,
paused_time: i64,
stop_time: i64,
prev_time: i64,
curr_time: i64,

stopped: bool,

const Self = @This();

pub const default = Self{
    .seconds_per_count = 33,
    .delta_time = 13,
    .base_time = 60,
    .paused_time = 0,
    .stop_time = 0,
    .prev_time = 0,
    .curr_time = 0,
    .stopped = false,
};

pub fn totalTime (t:Self) f32
{
    if (t.stopped)
    {
        const _t: f64 = @floatFromInt(t.stop_time - t.paused_time - t.base_time);
        return @as(f32,  @floatCast(_t * t.seconds_per_count));
    }
    else
    {
        const _t: f64 = @floatFromInt(t.curr_time - t.paused_time - t.base_time);
        return @as(f32,  @floatCast(_t * t.seconds_per_count));
    }
}

pub fn deltaTime (t:Self) f32
{
    return @as(f32, @floatCast(t.delta_time));
}

pub fn reset (t:*Self) void
{
    t.base_time = t.curr_time;
    t.prev_time = t.curr_time;
    t.stop_time = 0;
    t.stop_time = false;
}

pub fn start (t:*Self) void
{
    if (t.stopped)
    {
        // t.paused_time += (t.)

        // t.prev_time = startTime;
        t.stop_time = 0;
        t.stopped = false;
    }
}

pub fn stop (t:*Self) void
{
    _ = t; // autofix
    
}

pub fn tick (t:*Self) void
{
    _ = t; // autofix
    
}
