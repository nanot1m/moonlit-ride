// Short flowing descents, then assisted return climbs.
export const ROUTE_LENGTH=720, DESCENT_LENGTH=480;
export function elevation(z){const p=((-z%ROUTE_LENGTH)+ROUTE_LENGTH)%ROUTE_LENGTH;return p<DESCENT_LENGTH?8+22*(1+Math.cos(Math.PI*p/DESCENT_LENGTH))/2:8+22*(1-Math.cos(Math.PI*(p-DESCENT_LENGTH)/(ROUTE_LENGTH-DESCENT_LENGTH)))/2;}
export function slope(z){return (elevation(z+.05)-elevation(z-.05))/.1;}
