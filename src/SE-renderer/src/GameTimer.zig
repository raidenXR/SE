

seconds_per_count: f64,
delta_time: f64,

base_time:   i64,
paused_time: i64,
stop_time: i64,
prev_time: i64,
curr_time: i64,

stopped: bool,

const Self = @This();

pub fn totalTime (t:Self) f32
{
    if (t.stopped)
    {
        return @as(f32, @floatFromInt(((t.stop_time - t.paused_time) - t.base_time) * t.seconds_per_count));
    }
    else
    {
        return @as(f32, @floatFromInt(((t.curr_time - t.paused_time) - t.base_time) * t.seconds_per_count));
    }
}

pub fn deltaTime (t:Self) f32
{
    return @as(f32, @floatFromInt(t.delta_time));
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
